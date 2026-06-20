using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class BlendTreeParameterCollector
    {
        public readonly struct ParameterReference
        {
            public ParameterReference(string name)
            {
                Name = name != null ? name.Trim() : "";
            }

            public string Name { get; }
        }

        public static IEnumerable<string> Collect(Motion motion)
        {
            var seen = new HashSet<string>();
            foreach (var parameter in CollectParameterReferences(motion).Select(reference => reference.Name))
            {
                if (!string.IsNullOrWhiteSpace(parameter) && seen.Add(parameter))
                {
                    yield return parameter;
                }
            }
        }

        public static IEnumerable<ParameterReference> CollectParameterReferences(Motion motion)
        {
            if (motion is not BlendTree tree)
            {
                yield break;
            }

            if (UsesBlendParameter(tree))
            {
                yield return new ParameterReference(tree.blendParameter);
            }

            if (UsesBlendParameterY(tree))
            {
                yield return new ParameterReference(tree.blendParameterY);
            }

            foreach (var child in tree.children)
            {
                if (tree.blendType == BlendTreeType.Direct)
                {
                    yield return new ParameterReference(child.directBlendParameter);
                }

                foreach (var nested in CollectParameterReferences(child.motion))
                {
                    yield return nested;
                }
            }
        }

        private static bool UsesBlendParameter(BlendTree tree)
        {
            return tree.blendType != BlendTreeType.Direct;
        }

        private static bool UsesBlendParameterY(BlendTree tree)
        {
            return tree.blendType == BlendTreeType.SimpleDirectional2D ||
                   tree.blendType == BlendTreeType.FreeformCartesian2D ||
                   tree.blendType == BlendTreeType.FreeformDirectional2D;
        }
    }
}
