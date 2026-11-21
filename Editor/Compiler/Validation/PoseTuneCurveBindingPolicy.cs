using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneCurveBindingPolicy
    {
        public static bool HasUnsupportedCurves(AnimationClip clip)
        {
            return clip != null && HasUnsupportedCurves(clip, AnimationUtility.GetCurveBindings(clip));
        }

        /// <summary>
        /// <see cref="HasUnsupportedCurves(AnimationClip)"/> for a non-null <paramref name="clip"/> whose float curve
        /// bindings were already read. Object reference bindings are read only when every float binding is allowed.
        /// </summary>
        public static bool HasUnsupportedCurves(AnimationClip clip, IEnumerable<EditorCurveBinding> curveBindings)
        {
            return curveBindings.Any(binding => !IsAllowedFloatCurve(binding)) ||
                   AnimationUtility.GetObjectReferenceCurveBindings(clip)
                       .Any(binding => !IsAllowedObjectReferenceCurve(binding));
        }

        public static bool IsAllowedFloatCurve(EditorCurveBinding binding)
        {
            if (binding.type == typeof(Transform) || binding.type == typeof(Animator))
            {
                return true;
            }

            return binding.type == typeof(SkinnedMeshRenderer) &&
                   binding.propertyName.StartsWith("blendShape.");
        }

        public static bool IsAllowedObjectReferenceCurve(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform);
        }

        public static void RemoveUnsupportedCurves(AnimationClip generatedClip)
        {
            if (generatedClip == null)
            {
                return;
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(generatedClip)
                         .Where(binding => !IsAllowedFloatCurve(binding)))
            {
                AnimationUtility.SetEditorCurve(generatedClip, binding, null);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(generatedClip)
                         .Where(binding => !IsAllowedObjectReferenceCurve(binding)))
            {
                AnimationUtility.SetObjectReferenceCurve(generatedClip, binding, null);
            }
        }
    }
}
