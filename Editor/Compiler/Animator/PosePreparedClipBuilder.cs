using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Validation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PosePreparedClipBuilder
    {
        public static bool RequiresPreparedMotion(PoseDefinition pose)
        {
            if (pose == null)
            {
                return false;
            }

            if (pose.AdjustmentClip != null ||
                pose.RootOffset != Vector3.zero ||
                !Mathf.Approximately(pose.RootYawOffsetDegrees, 0f) ||
                !Mathf.Approximately(pose.HumanoidOrientationOffsetYDegrees, 0f) ||
                pose.RecenterRootXZToHead)
            {
                return true;
            }

            return LeafClips(pose.SourceMotion != null ? pose.SourceMotion : pose.Clip)
                .Any(clip => AnimationUtility.GetAnimationClipSettings(clip).loopTime != pose.Loop);
        }

        public static AnimationClip ClonePreparedClip(
            PoseDefinition pose,
            string name,
            bool sanitizeUnsupportedSourceCurves = false)
        {
            var source = pose.SourceMotion as AnimationClip ?? pose.Clip;
            return ClonePreparedClip(pose, source, name, sanitizeUnsupportedSourceCurves);
        }

        public static AnimationClip ClonePreparedClip(
            PoseDefinition pose,
            AnimationClip source,
            string name,
            bool sanitizeUnsupportedSourceCurves = false)
        {
            var clip = CloneClip(source, name);
            if (sanitizeUnsupportedSourceCurves)
            {
                PoseTuneCurveBindingPolicy.RemoveUnsupportedCurves(clip);
            }

            ApplyAdjustmentClip(clip, pose.AdjustmentClip, pose.AdjustmentApplyMode);
            ApplyRootOffset(clip, pose.RootOffset);
            ApplyHumanoidOrientationOffsetY(clip, pose.HumanoidOrientationOffsetYDegrees);
            ApplyRootYawOffset(clip, pose.RootYawOffsetDegrees);
            if (pose.RecenterRootXZToHead)
            {
                RecenterRootXZ(clip);
            }

            ApplyLoopSetting(clip, pose.Loop);
            return clip;
        }

        private static AnimationClip CloneClip(AnimationClip source, string name)
        {
            var clip = Object.Instantiate(source);
            clip.name = name;
            return clip;
        }

        private static IEnumerable<AnimationClip> LeafClips(Motion motion)
        {
            if (motion is AnimationClip clip)
            {
                yield return clip;
                yield break;
            }

            if (motion is not UnityEditor.Animations.BlendTree tree)
            {
                yield break;
            }

            foreach (var child in tree.children)
            {
                foreach (var leaf in LeafClips(child.motion))
                {
                    yield return leaf;
                }
            }
        }

        private static void ApplyAdjustmentClip(
            AnimationClip target,
            AnimationClip adjustment,
            PoseAdjustmentApplyMode applyMode)
        {
            if (adjustment == null)
            {
                return;
            }

            var rootQBindings = new List<EditorCurveBinding>();
            foreach (var binding in AnimationUtility.GetCurveBindings(adjustment))
            {
                var curve = AnimationUtility.GetEditorCurve(adjustment, binding);
                if (curve == null)
                {
                    continue;
                }

                if (applyMode == PoseAdjustmentApplyMode.AdditiveKawaiiCompatible &&
                    PoseRootMotionCurveUtility.IsRootQ(binding))
                {
                    rootQBindings.Add(binding);
                    continue;
                }

                if (applyMode == PoseAdjustmentApplyMode.AdditiveKawaiiCompatible &&
                    PoseRootMotionCurveUtility.IsRootT(binding))
                {
                    PoseRootMotionCurveUtility.AddFloatCurve(target, binding, curve);
                    continue;
                }

                AnimationUtility.SetEditorCurve(target, binding, PoseRootMotionCurveUtility.CopyCurve(curve));
            }

            if (rootQBindings.Count > 0)
            {
                PoseRootMotionCurveUtility.MultiplyRootQuaternion(target, adjustment);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(adjustment))
            {
                var frames = AnimationUtility.GetObjectReferenceCurve(adjustment, binding);
                if (frames == null)
                {
                    continue;
                }

                var copiedFrames = new ObjectReferenceKeyframe[frames.Length];
                for (var i = 0; i < frames.Length; i++)
                {
                    copiedFrames[i] = frames[i];
                }

                AnimationUtility.SetObjectReferenceCurve(target, binding, copiedFrames);
            }
        }

        private static void ApplyLoopSetting(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void ApplyRootOffset(AnimationClip clip, Vector3 offset)
        {
            if (offset == Vector3.zero)
            {
                return;
            }

            PoseRootMotionCurveUtility.ApplyRootCurveOffset(clip, "RootT.x", offset.x);
            PoseRootMotionCurveUtility.ApplyRootCurveOffset(clip, "RootT.y", offset.y);
            PoseRootMotionCurveUtility.ApplyRootCurveOffset(clip, "RootT.z", offset.z);
        }

        private static void ApplyRootYawOffset(AnimationClip clip, float degrees)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            PoseRootMotionCurveUtility.MultiplyRootQuaternion(clip, Quaternion.Euler(0f, degrees, 0f));
        }

        private static void ApplyHumanoidOrientationOffsetY(AnimationClip clip, float degrees)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.orientationOffsetY += degrees;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void RecenterRootXZ(AnimationClip clip)
        {
            RecenterRootCurve(clip, "RootT.x");
            RecenterRootCurve(clip, "RootT.z");
        }

        private static void RecenterRootCurve(AnimationClip clip, string propertyName)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.keys.Length == 0)
            {
                return;
            }

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                PoseRootMotionCurveUtility.OffsetCurve(curve, -curve.Evaluate(0f)));
        }
    }
}
