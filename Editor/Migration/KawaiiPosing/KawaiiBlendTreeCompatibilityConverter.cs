using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiBlendTreeCompatibilityConverter
    {
        public static BlendTree CloneForBuild(BlendTree source, string name)
        {
            return source != null ? PoseMotionPreparationService.CloneBlendTree(source, name) : null;
        }

        public static bool IsBlendTree(Motion motion)
        {
            return motion is BlendTree;
        }

        public static IReadOnlyList<KawaiiAnimationDto> ExpandAnimation(
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options)
        {
            if (source == null)
            {
                return System.Array.Empty<KawaiiAnimationDto>();
            }

            if (options == null ||
                options.blendTreeMode != KawaiiBlendTreeMode.FlattenLeaves ||
                source.BlendTree == null)
            {
                return new[] { source };
            }

            var leaves = LeafClips(source.BlendTree).ToList();
            if (leaves.Count == 0)
            {
                return new[] { source };
            }

            var result = new List<KawaiiAnimationDto>();
            for (var i = 0; i < leaves.Count; i++)
            {
                var leaf = leaves[i];
                result.Add(CloneForLeaf(source, leaf, i, leaves.Count));
            }

            return result;
        }

        private static KawaiiAnimationDto CloneForLeaf(
            KawaiiAnimationDto source,
            AnimationClip leaf,
            int leafIndex,
            int leafCount)
        {
            var suffix = leafCount > 1
                ? " / " + (!string.IsNullOrWhiteSpace(leaf.name) ? leaf.name : "Leaf " + leafIndex)
                : "";
            var baseName = KawaiiPosingMapper.DisplayName(source);
            return new KawaiiAnimationDto
            {
                Index = source.Index + leafIndex,
                Enabled = source.Enabled,
                IsRotate = source.IsRotate,
                Rotate = source.Rotate,
                IsMotionTime = source.IsMotionTime,
                MotionTimeParameterName = source.MotionTimeParameterName,
                Motion = leaf,
                Clip = leaf,
                BlendTree = null,
                PreviewImage = source.PreviewImage,
                AdjustmentClip = source.AdjustmentClip,
                DisplayName = baseName + suffix,
                Initial = source.Initial && leafIndex == 0,
                InitialSet = source.InitialSet && leafIndex == 0,
                IsCustomIcon = source.IsCustomIcon,
                Icon = source.Icon,
                TypeParameterValue = source.TypeParameterValue > 0 ? source.TypeParameterValue + leafIndex : 0,
                SyncedParameterValue = source.SyncedParameterValue > 0 ? source.SyncedParameterValue + leafIndex : 0
            };
        }

        private static IEnumerable<AnimationClip> LeafClips(Motion motion)
        {
            if (motion is AnimationClip clip)
            {
                yield return clip;
                yield break;
            }

            if (motion is not BlendTree tree)
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
    }
}
