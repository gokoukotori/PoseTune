using System.Linq;
using Gokoukotori.PoseTune.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneClipValidator
    {
        public static void Validate(PoseDefinition pose, ValidationReport report)
        {
            if (pose.Clip.empty || Mathf.Approximately(pose.Clip.length, 0f))
            {
                report.Warning(PoseTuneDiagnostics.ClipZeroLength.Code, "AnimationClip の長さが 0 です。", pose.Source);
            }

            var floatBindings = AnimationUtility.GetCurveBindings(pose.Clip);
            if (floatBindings.Any(IsRootTransformCurve))
            {
                report.Warning(PoseTuneDiagnostics.ClipRootTransformCurves.Code, "AnimationClip に root transform curve が含まれています。", pose.Source);
            }

            if (floatBindings.Any(IsUnsupportedCurveBinding) ||
                AnimationUtility.GetObjectReferenceCurveBindings(pose.Clip).Any(IsUnsupportedObjectReferenceBinding))
            {
                report.Error(PoseTuneDiagnostics.ClipUnsupportedCurves.Code, "Base / Action target の PoseClip には Transform / Animator 以外の curve を含められません。", pose.Source);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(pose.Clip);
            if (settings.loopTime != pose.Loop)
            {
                report.Warning(PoseTuneDiagnostics.ClipLoopMismatch.Code, "PoseClip のループ設定が AnimationClip のループ設定と異なります。", pose.Source);
            }
        }

        public static void ValidateMotion(PoseDefinition pose, ValidationReport report)
        {
            if (pose?.SourceMotion is BlendTree tree)
            {
                foreach (var clip in MotionTreeCloneUtility.EnumerateMotions(tree).OfType<AnimationClip>())
                {
                    Validate(ClipPose(pose, clip), report);
                }

                return;
            }

            if (pose?.Clip != null)
            {
                Validate(pose, report);
            }
        }

        private static bool IsRootTransformCurve(EditorCurveBinding binding)
        {
            if (!string.IsNullOrEmpty(binding.path))
            {
                return false;
            }

            return binding.type == typeof(Transform) ||
                   (binding.type == typeof(Animator) && binding.propertyName.StartsWith("RootT"));
        }

        private static bool IsUnsupportedCurveBinding(EditorCurveBinding binding)
        {
            if (binding.type == typeof(Transform) || binding.type == typeof(Animator))
            {
                return false;
            }

            if (binding.type == typeof(SkinnedMeshRenderer) &&
                binding.propertyName.StartsWith("blendShape."))
            {
                return false;
            }

            return true;
        }

        private static bool IsUnsupportedObjectReferenceBinding(EditorCurveBinding binding)
        {
            return binding.type != typeof(Transform);
        }

        private static PoseDefinition ClipPose(PoseDefinition source, AnimationClip clip)
        {
            return new PoseDefinition
            {
                Clip = clip,
                Source = source?.Source,
                Loop = source != null && source.Loop
            };
        }
    }
}
