using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseRootMotionCurveUtility
    {
        public static AnimationCurve OffsetCurve(AnimationCurve source, float offset)
        {
            if (source.keys.Length == 0)
            {
                return AnimationCurve.Constant(0f, 0f, offset);
            }

            var keys = new Keyframe[source.keys.Length];
            for (var i = 0; i < source.keys.Length; i++)
            {
                var key = source.keys[i];
                key.value += offset;
                keys[i] = key;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        public static AnimationCurve CopyCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        public static void ApplyRootCurveOffset(AnimationClip clip, string propertyName, float offset)
        {
            if (Mathf.Approximately(offset, 0f))
            {
                return;
            }

            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            var existing = AnimationUtility.GetEditorCurve(clip, binding);
            var curve = existing != null
                ? OffsetCurve(existing, offset)
                : AnimationCurve.Constant(0f, Mathf.Max(0f, clip.length), offset);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        public static void AddFloatCurve(AnimationClip target, EditorCurveBinding binding, AnimationCurve addition)
        {
            var existing = AnimationUtility.GetEditorCurve(target, binding);
            if (existing == null)
            {
                AnimationUtility.SetEditorCurve(target, binding, CopyCurve(addition));
                return;
            }

            var times = CurveTimes(existing).Union(CurveTimes(addition)).OrderBy(t => t).ToArray();
            if (times.Length == 0)
            {
                times = new[] { 0f, Mathf.Max(existing.length, addition.length) };
            }

            var keys = times.Select(time => new Keyframe(time, existing.Evaluate(time) + addition.Evaluate(time))).ToArray();
            AnimationUtility.SetEditorCurve(target, binding, new AnimationCurve(keys)
            {
                preWrapMode = existing.preWrapMode,
                postWrapMode = existing.postWrapMode
            });
        }

        public static void MultiplyRootQuaternion(AnimationClip target, AnimationClip adjustment)
        {
            var bindings = RootQBindings();
            var targetCurves = bindings.ToDictionary(pair => pair.Key, pair => AnimationUtility.GetEditorCurve(target, pair.Value));
            var adjustmentCurves = bindings.ToDictionary(pair => pair.Key, pair => AnimationUtility.GetEditorCurve(adjustment, pair.Value));
            var times = targetCurves.Values.Concat(adjustmentCurves.Values)
                .Where(curve => curve != null)
                .SelectMany(CurveTimes)
                .DefaultIfEmpty(0f)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            var output = NewQuaternionCurveSet();
            foreach (var time in times)
            {
                var targetQ = EvaluateQuaternion(targetCurves, time);
                var adjustmentQ = EvaluateQuaternion(adjustmentCurves, time);
                AddQuaternionKey(output, time, targetQ * adjustmentQ);
            }

            WriteRootQCurves(target, output);
        }

        public static void MultiplyRootQuaternion(AnimationClip target, Quaternion offset)
        {
            var bindings = RootQBindings();
            var targetCurves = bindings.ToDictionary(pair => pair.Key, pair => AnimationUtility.GetEditorCurve(target, pair.Value));
            var times = targetCurves.Values
                .Where(curve => curve != null)
                .SelectMany(CurveTimes)
                .DefaultIfEmpty(0f)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            var output = NewQuaternionCurveSet();
            foreach (var time in times)
            {
                AddQuaternionKey(output, time, offset * EvaluateQuaternion(targetCurves, time));
            }

            WriteRootQCurves(target, output);
        }

        public static bool IsRootT(EditorCurveBinding binding)
        {
            return string.IsNullOrEmpty(binding.path) &&
                   binding.type == typeof(Animator) &&
                   binding.propertyName.StartsWith("RootT.");
        }

        public static bool IsRootQ(EditorCurveBinding binding)
        {
            return string.IsNullOrEmpty(binding.path) &&
                   binding.type == typeof(Animator) &&
                   binding.propertyName.StartsWith("RootQ.");
        }

        private static IEnumerable<float> CurveTimes(AnimationCurve curve)
        {
            return curve != null ? curve.keys.Select(key => key.time) : Enumerable.Empty<float>();
        }

        private static Dictionary<string, AnimationCurve> NewQuaternionCurveSet()
        {
            return new Dictionary<string, AnimationCurve>
            {
                { "x", new AnimationCurve() },
                { "y", new AnimationCurve() },
                { "z", new AnimationCurve() },
                { "w", new AnimationCurve() }
            };
        }

        private static void AddQuaternionKey(Dictionary<string, AnimationCurve> curves, float time, Quaternion value)
        {
            value = Normalize(value);
            curves["x"].AddKey(time, value.x);
            curves["y"].AddKey(time, value.y);
            curves["z"].AddKey(time, value.z);
            curves["w"].AddKey(time, value.w);
        }

        private static Quaternion EvaluateQuaternion(Dictionary<string, AnimationCurve> curves, float time)
        {
            var q = new Quaternion(
                curves["x"] != null ? curves["x"].Evaluate(time) : 0f,
                curves["y"] != null ? curves["y"].Evaluate(time) : 0f,
                curves["z"] != null ? curves["z"].Evaluate(time) : 0f,
                curves["w"] != null ? curves["w"].Evaluate(time) : 1f);
            return Normalize(q);
        }

        private static Quaternion Normalize(Quaternion q)
        {
            var length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (length <= 0.00001f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(q.x / length, q.y / length, q.z / length, q.w / length);
        }

        private static void WriteRootQCurves(AnimationClip target, Dictionary<string, AnimationCurve> curves)
        {
            foreach (var pair in RootQBindings())
            {
                AnimationUtility.SetEditorCurve(target, pair.Value, curves[pair.Key]);
            }
        }

        private static Dictionary<string, EditorCurveBinding> RootQBindings()
        {
            return new Dictionary<string, EditorCurveBinding>
            {
                { "x", EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x") },
                { "y", EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.y") },
                { "z", EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.z") },
                { "w", EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w") }
            };
        }
    }
}
