using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class BlendTreeParameterCollector
    {
        public static IEnumerable<string> Collect(Motion motion)
        {
            var seen = new HashSet<string>();
            foreach (var parameter in CollectRecursive(motion))
            {
                if (!string.IsNullOrWhiteSpace(parameter) && seen.Add(parameter))
                {
                    yield return parameter;
                }
            }
        }

        private static IEnumerable<string> CollectRecursive(Motion motion)
        {
            if (motion is not BlendTree tree)
            {
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(tree.blendParameter))
            {
                yield return tree.blendParameter.Trim();
            }

            if (!string.IsNullOrWhiteSpace(tree.blendParameterY))
            {
                yield return tree.blendParameterY.Trim();
            }

            foreach (var child in tree.children)
            {
                if (!string.IsNullOrWhiteSpace(child.directBlendParameter))
                {
                    yield return child.directBlendParameter.Trim();
                }

                foreach (var nested in CollectRecursive(child.motion))
                {
                    yield return nested;
                }
            }
        }
    }
}
