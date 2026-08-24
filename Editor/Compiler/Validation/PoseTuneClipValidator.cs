using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneClipValidator
    {
        private const float PositionTolerance = 0.00001f;
        private const float RotationToleranceDegrees = 0.01f;

        public static void Validate(PoseDefinition pose, ValidationReport report)
        {
            if (pose.Clip.empty)
            {
                report.Warning(PoseTuneDiagnostics.ClipZeroLength.Code, "AnimationClip に有効な curve がありません。", pose.Source);
            }
            else if (Mathf.Approximately(pose.Clip.length, 0f) &&
                     pose.MotionTime != null &&
                     pose.MotionTime.mode != MotionTimeMode.None)
            {
                report.Warning(PoseTuneDiagnostics.ClipZeroLength.Code, "Motion Time を使用していますが AnimationClip の長さが 0 です。", pose.Source);
            }

            var floatBindings = AnimationUtility.GetCurveBindings(pose.Clip);
            var rootCurveSets = CollectRootCurveSets(pose.Clip, floatBindings);
            if (rootCurveSets.Any(set => set.HasTimeVariation()))
            {
                report.Warning(PoseTuneDiagnostics.ClipRootTransformCurves.Code,
                    "AnimationClip に時間変化する root position / rotation curve が含まれています。", pose.Source);
            }
            else if (pose.CompatibilityProfile != PoseSourceCompatibilityProfile.KawaiiPosing &&
                     rootCurveSets.Any(set => set.HasNonIdentityValue()))
            {
                report.Warning(PoseTuneDiagnostics.ClipRootTransformCurves.Code,
                    "AnimationClip に静的な root position / rotation offset が含まれています。", pose.Source);
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

        private static List<RootCurveSet> CollectRootCurveSets(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings)
        {
            var sets = new Dictionary<string, RootCurveSet>();
            foreach (var binding in bindings)
            {
                if (!TryClassifyRootCurve(binding, out var kind, out var family, out var component))
                {
                    continue;
                }

                if (!sets.TryGetValue(family, out var set))
                {
                    set = new RootCurveSet(kind);
                    sets.Add(family, set);
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                {
                    set.Curves[component] = curve;
                }
            }

            return sets.Values.ToList();
        }

        private static bool TryClassifyRootCurve(
            EditorCurveBinding binding,
            out RootCurveKind kind,
            out string family,
            out string component)
        {
            kind = RootCurveKind.Position;
            family = "";
            component = "";
            if (!string.IsNullOrEmpty(binding.path) || string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            if (PoseRootMotionCurveUtility.IsRootT(binding))
            {
                kind = RootCurveKind.Position;
                family = "Animator.RootT";
                component = ComponentName(binding.propertyName);
                return true;
            }

            if (PoseRootMotionCurveUtility.IsRootQ(binding))
            {
                kind = RootCurveKind.Quaternion;
                family = "Animator.RootQ";
                component = ComponentName(binding.propertyName);
                return true;
            }

            if (binding.type != typeof(Transform))
            {
                return false;
            }

            if (TryMatchTransformProperty(binding.propertyName,
                    new[] { "m_LocalPosition.", "localPosition." }, out component, out family))
            {
                kind = RootCurveKind.Position;
                return true;
            }

            if (TryMatchTransformProperty(binding.propertyName,
                    new[] { "m_LocalRotation.", "localRotation." }, out component, out family))
            {
                kind = RootCurveKind.Quaternion;
                return true;
            }

            if (TryMatchTransformProperty(binding.propertyName,
                    new[]
                    {
                        "localEulerAnglesRaw.",
                        "localEulerAnglesBaked.",
                        "localEulerAngles.",
                        "m_LocalEulerAngles."
                    }, out component, out family))
            {
                kind = RootCurveKind.Euler;
                return true;
            }

            return false;
        }

        private static bool TryMatchTransformProperty(
            string propertyName,
            IEnumerable<string> prefixes,
            out string component,
            out string family)
        {
            foreach (var prefix in prefixes)
            {
                if (!propertyName.StartsWith(prefix))
                {
                    continue;
                }

                component = propertyName.Substring(prefix.Length);
                family = "Transform." + prefix.TrimEnd('.');
                return true;
            }

            component = "";
            family = "";
            return false;
        }

        private static string ComponentName(string propertyName)
        {
            var separator = propertyName.LastIndexOf('.');
            return separator >= 0 ? propertyName.Substring(separator + 1) : propertyName;
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
                Loop = source != null && source.Loop,
                CompatibilityProfile = source != null
                    ? source.CompatibilityProfile
                    : PoseSourceCompatibilityProfile.None,
                MotionTime = source?.MotionTime ?? new MotionTimeSettings()
            };
        }

        private enum RootCurveKind
        {
            Position,
            Quaternion,
            Euler
        }

        private sealed class RootCurveSet
        {
            public RootCurveSet(RootCurveKind kind)
            {
                Kind = kind;
            }

            public RootCurveKind Kind { get; }
            public Dictionary<string, AnimationCurve> Curves { get; } = new();

            public bool HasTimeVariation()
            {
                var times = KeyTimes();
                if (times.Length <= 1)
                {
                    return false;
                }

                switch (Kind)
                {
                    case RootCurveKind.Position:
                    {
                        var first = EvaluateVector(times[0]);
                        return times.Skip(1).Any(time =>
                            (EvaluateVector(time) - first).sqrMagnitude > PositionTolerance * PositionTolerance);
                    }
                    case RootCurveKind.Quaternion:
                    {
                        var first = EvaluateQuaternion(times[0]);
                        return times.Skip(1).Any(time =>
                            Quaternion.Angle(first, EvaluateQuaternion(time)) > RotationToleranceDegrees);
                    }
                    case RootCurveKind.Euler:
                    {
                        var first = EvaluateVector(times[0]);
                        return times.Skip(1).Any(time => HasEulerDifference(first, EvaluateVector(time)));
                    }
                    default:
                        return false;
                }
            }

            public bool HasNonIdentityValue()
            {
                var time = KeyTimes().FirstOrDefault();
                switch (Kind)
                {
                    case RootCurveKind.Position:
                        return EvaluateVector(time).sqrMagnitude > PositionTolerance * PositionTolerance;
                    case RootCurveKind.Quaternion:
                        return Quaternion.Angle(Quaternion.identity, EvaluateQuaternion(time)) > RotationToleranceDegrees;
                    case RootCurveKind.Euler:
                        return HasEulerDifference(Vector3.zero, EvaluateVector(time));
                    default:
                        return false;
                }
            }

            private float[] KeyTimes()
            {
                return Curves.Values
                    .SelectMany(curve => curve.keys.Select(key => key.time))
                    .Distinct()
                    .OrderBy(time => time)
                    .ToArray();
            }

            private Vector3 EvaluateVector(float time)
            {
                return new Vector3(Evaluate("x", time, 0f), Evaluate("y", time, 0f), Evaluate("z", time, 0f));
            }

            private Quaternion EvaluateQuaternion(float time)
            {
                var value = new Quaternion(
                    Evaluate("x", time, 0f),
                    Evaluate("y", time, 0f),
                    Evaluate("z", time, 0f),
                    Evaluate("w", time, 1f));
                var length = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
                if (length <= PositionTolerance)
                {
                    return Quaternion.identity;
                }

                return new Quaternion(value.x / length, value.y / length, value.z / length, value.w / length);
            }

            private float Evaluate(string component, float time, float fallback)
            {
                return Curves.TryGetValue(component, out var curve) ? curve.Evaluate(time) : fallback;
            }

            private static bool HasEulerDifference(Vector3 first, Vector3 second)
            {
                return Mathf.Abs(Mathf.DeltaAngle(first.x, second.x)) > RotationToleranceDegrees ||
                       Mathf.Abs(Mathf.DeltaAngle(first.y, second.y)) > RotationToleranceDegrees ||
                       Mathf.Abs(Mathf.DeltaAngle(first.z, second.z)) > RotationToleranceDegrees;
            }
        }
    }
}
