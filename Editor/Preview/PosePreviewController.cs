using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PosePreviewController
    {
        private static GameObject previewRoot;
        private static GameObject previewClone;
        private static GameObject currentAvatar;
        private static bool startedAnimationMode;

        public static GameObject CurrentRoot => previewRoot;
        public static GameObject CurrentClone => previewClone;
        public static GameObject CurrentAvatar => currentAvatar;
        public static bool IsPreviewActive => startedAnimationMode && currentAvatar != null && AnimationMode.InAnimationMode();

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

            if (previewRoot != null)
            {
                Object.DestroyImmediate(previewRoot);
                previewRoot = null;
                previewClone = null;
            }
            else if (previewClone != null)
            {
                Object.DestroyImmediate(previewClone);
                previewClone = null;
            }

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
