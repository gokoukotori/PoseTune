using Gokoukotori.PoseTune;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseGroundingAnalyzeOptions
    {
        public bool UseRendererBounds = true;
        public bool UseAvatarRootFloorY = true;
        public float FloorY;
    }

    internal sealed class PoseGroundingSuggestion
    {
        public float SuggestedRootYOffset;
        public Vector3 SuggestedCameraOffset;
        public string Reason = "";
        public bool RequiresManualReview;
    }

    internal sealed class PoseGroundingAnalyzer
    {
        public PoseGroundingSuggestion Analyze(
            PoseClip pose,
            PoseTuneRoot root,
            PoseGroundingAnalyzeOptions options)
        {
            options ??= new PoseGroundingAnalyzeOptions();
            if (pose == null)
            {
                return new PoseGroundingSuggestion
                {
                    Reason = "PoseClip is null.",
                    RequiresManualReview = true
                };
            }

            if (!options.UseRendererBounds)
            {
                return CurrentOffsetSuggestion(
                    pose,
                    "Current PoseClip offsets are used because renderer bounds analysis is disabled.",
                    IsManualReviewPose(pose));
            }

            var avatarRoot = ResolveAvatarRoot(root, pose);
            var sampleClip = PoseClipPreparationService.PrepareClipForSampling(pose, pose.displayName + "_GroundingSample");
            if (avatarRoot == null || sampleClip == null)
            {
                return CurrentOffsetSuggestion(
                    pose,
                    "Current PoseClip offsets are used because the avatar root or sample clip could not be resolved.",
                    true);
            }

            GameObject clone = null;
            try
            {
                clone = Object.Instantiate(avatarRoot);
                clone.name = avatarRoot.name + "_PoseTuneGroundingSample";
                clone.hideFlags = HideFlags.HideAndDontSave;
                SetHideFlags(clone.transform, HideFlags.HideAndDontSave);

                sampleClip.SampleAnimation(clone, 0f);
                if (!TryGetLowestRendererY(clone, out var lowestY))
                {
                    return CurrentOffsetSuggestion(
                        pose,
                        "Current PoseClip offsets are used because renderer bounds could not be sampled.",
                        true);
                }

                var floorY = options.UseAvatarRootFloorY ? avatarRoot.transform.position.y : options.FloorY;
                var deltaToFloor = floorY - lowestY;
                return new PoseGroundingSuggestion
                {
                    SuggestedRootYOffset = pose.rootOffset.y + deltaToFloor,
                    SuggestedCameraOffset = pose.cameraOffset,
                    Reason = $"renderer bounds lowest Y {lowestY:0.###}; floor Y {floorY:0.###}.",
                    RequiresManualReview = IsManualReviewPose(pose)
                };
            }
            finally
            {
                if (clone != null)
                {
                    Object.DestroyImmediate(clone);
                }

                PoseClipPreparationService.ReleasePreparedClipForSampling(sampleClip);
            }
        }

        private static PoseGroundingSuggestion CurrentOffsetSuggestion(
            PoseClip pose,
            string reason,
            bool requiresManualReview)
        {
            return new PoseGroundingSuggestion
            {
                SuggestedRootYOffset = pose.rootOffset.y,
                SuggestedCameraOffset = pose.cameraOffset,
                Reason = reason,
                RequiresManualReview = requiresManualReview
            };
        }

        private static GameObject ResolveAvatarRoot(PoseTuneRoot root, PoseClip pose)
        {
            var avatar = root != null
                ? root.GetComponentInParent<VRCAvatarDescriptor>(true)
                : pose.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (avatar != null)
            {
                return avatar.gameObject;
            }

            if (root != null)
            {
                return root.transform.root.gameObject;
            }

            return pose.transform.root.gameObject;
        }

        private static bool TryGetLowestRendererY(GameObject root, out float lowestY)
        {
            lowestY = float.PositiveInfinity;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                lowestY = Mathf.Min(lowestY, renderer.bounds.min.y);
            }

            return !float.IsPositiveInfinity(lowestY);
        }

        private static bool IsManualReviewPose(PoseClip pose)
        {
            return pose.GetComponentInParent<PoseGroup>()?.kind is PoseGroupKind.Prone or PoseGroupKind.Supine;
        }

        private static void SetHideFlags(Transform transform, HideFlags hideFlags)
        {
            transform.gameObject.hideFlags = hideFlags;

            for (var i = 0; i < transform.childCount; i++)
            {
                SetHideFlags(transform.GetChild(i), hideFlags);
            }
        }
    }
}
