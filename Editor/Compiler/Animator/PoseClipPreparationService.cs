using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseClipPreparationService
    {
        public static AnimationClip PrepareClipForSampling(PoseClip pose, string name)
        {
            if (pose == null)
            {
                return null;
            }

            var previewClip = FirstLeafClip(pose.sourceMotion) ?? pose.clip;
            if (previewClip == null)
            {
                return null;
            }

            var definition = new PoseDefinition
            {
                DisplayName = string.IsNullOrWhiteSpace(pose.displayName) ? previewClip.name : pose.displayName,
                Clip = previewClip,
                SourceMotion = previewClip,
                AdjustmentClip = pose.adjustmentClip,
                AdjustmentApplyMode = pose.adjustmentApplyMode,
                RootOffset = pose.rootOffset,
                RootYawOffsetDegrees = pose.rootYawOffsetDegrees,
                HumanoidOrientationOffsetYDegrees = pose.humanoidOrientationOffsetYDegrees,
                RecenterRootXZToHead = pose.recenterRootXZToHead,
                Loop = pose.loop
            };
            var result = PoseMotionPreparationService.PrepareMotion(
                definition,
                name,
                PoseMotionPreparationContext.Empty());
            if (result.Motion is AnimationClip clip)
            {
                clip.hideFlags = result.GeneratedAssets.Count > 0 ? HideFlags.HideAndDontSave : clip.hideFlags;
                return clip;
            }

            return previewClip;
        }

        public static void ReleasePreparedClipForSampling(AnimationClip clip)
        {
            if (clip != null && (clip.hideFlags & HideFlags.DontSave) != 0)
            {
                Object.DestroyImmediate(clip);
            }
        }

        private static AnimationClip FirstLeafClip(Motion motion)
        {
            if (motion is AnimationClip clip)
            {
                return clip;
            }

            if (motion is not UnityEditor.Animations.BlendTree tree)
            {
                return null;
            }

            foreach (var child in tree.children)
            {
                var leaf = FirstLeafClip(child.motion);
                if (leaf != null)
                {
                    return leaf;
                }
            }

            return null;
        }

        public static bool RequiresPreparedClip(PoseDefinition pose)
        {
            return PosePreparedClipBuilder.RequiresPreparedMotion(pose);
        }

        public static AnimationClip ClonePreparedClip(PoseDefinition pose, string name)
        {
            return PosePreparedClipBuilder.ClonePreparedClip(pose, name);
        }

        public static AnimationCurve OffsetCurve(AnimationCurve source, float offset)
        {
            return PoseRootMotionCurveUtility.OffsetCurve(source, offset);
        }
    }
}
