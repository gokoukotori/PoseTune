using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class MotionTreeCloneUtility
    {
        public static Motion Clone(
            Motion source,
            string name,
            IList<Object> assets,
            Func<AnimationClip, string, AnimationClip> clipTransformer)
        {
            return Clone(
                source,
                name,
                assets,
                clipTransformer,
                (motion, parentName, index) => parentName + "_" + index);
        }

        public static Motion Clone(
            Motion source,
            string name,
            IList<Object> assets,
            Func<AnimationClip, string, AnimationClip> clipTransformer,
            Func<Motion, string, int, string> childNameSelector)
        {
            if (source is BlendTree tree)
            {
                var clone = new BlendTree
                {
                    name = name,
                    blendType = tree.blendType,
                    blendParameter = tree.blendParameter,
                    blendParameterY = tree.blendParameterY,
                    minThreshold = tree.minThreshold,
                    maxThreshold = tree.maxThreshold,
                    useAutomaticThresholds = tree.useAutomaticThresholds
                };
                var children = tree.children;
                var copied = new ChildMotion[children.Length];
                for (var i = 0; i < children.Length; i++)
                {
                    copied[i] = children[i];
                    var childName = childNameSelector != null
                        ? childNameSelector(children[i].motion, name, i)
                        : name + "_" + i;
                    copied[i].motion = Clone(
                        children[i].motion,
                        childName,
                        assets,
                        clipTransformer,
                        childNameSelector);
                }

                clone.children = copied;
                assets.Add(clone);
                return clone;
            }

            if (source is AnimationClip clip)
            {
                var cloned = clipTransformer(clip, name);
                assets.Add(cloned);
                return cloned;
            }

            return source;
        }

        internal static IEnumerable<Motion> EnumerateMotions(Motion motion)
        {
            if (motion == null)
            {
                yield break;
            }

            yield return motion;
            if (motion is not BlendTree tree)
            {
                yield break;
            }

            foreach (var child in tree.children)
            {
                foreach (var nested in EnumerateMotions(child.motion))
                {
                    yield return nested;
                }
            }
        }
    }
}
