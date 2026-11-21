using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PosePreviewController
    {
        private static GameObject currentAvatar;
        private static bool startedAnimationMode;

        public static void ApplyPreview(PoseClip pose)
        {
            if (startedAnimationMode)
            {
                ResetPreview();
            }
            else if (AnimationMode.InAnimationMode())
            {
                return;
            }

            if (pose == null)
            {
                return;
            }

            var avatar = ResolveAvatar(pose);
            if (avatar == null)
            {
                return;
            }

            var sampleClip = PoseClipPreparationService.PrepareClipForSampling(pose, pose.displayName + "_Preview");
            if (sampleClip == null)
            {
                return;
            }

            AnimationMode.StartAnimationMode();
            startedAnimationMode = true;
            currentAvatar = avatar.gameObject;
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(currentAvatar, sampleClip, 0f);
            }
            finally
            {
                AnimationMode.EndSampling();
                PoseClipPreparationService.ReleasePreparedClipForSampling(sampleClip);
            }

            SceneView.RepaintAll();
        }

        public static void ResetPreview()
        {
            if (startedAnimationMode && AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            startedAnimationMode = false;
            currentAvatar = null;

            SceneView.RepaintAll();
        }

        private static VRCAvatarDescriptor ResolveAvatar(Component component)
        {
            var root = component.GetComponentInParent<PoseTuneRoot>(true);
            return root != null
                ? root.GetComponentInParent<VRCAvatarDescriptor>(true)
                : component.GetComponentInParent<VRCAvatarDescriptor>(true);
        }
    }
}
