using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class AnimatorBuildResult
    {
        public AnimatorController TargetController;
        public AnimatorController FxController;
        public ParameterPlan Parameters;
        public List<Object> GeneratedAssets = new();

        public IEnumerable<Object> EnumerateGeneratedAssets()
        {
            var seen = new HashSet<Object>(ReferenceEqualityComparer<Object>.Instance);
            foreach (var asset in GeneratedAssets)
            {
                if (asset != null && seen.Add(asset))
                {
                    yield return asset;
                }
            }

            if (TargetController != null && seen.Add(TargetController))
            {
                yield return TargetController;
            }

            if (HasAnimatorContent(FxController) && seen.Add(FxController))
            {
                yield return FxController;
            }
        }

        private static bool HasAnimatorContent(AnimatorController controller)
        {
            return controller != null && controller.layers != null && controller.layers.Length > 0;
        }
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
