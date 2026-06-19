using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseGroundingPresetFactory
    {
        public static AvatarAdjustmentPreset CreateAdjustmentPreset(
            string avatarName,
            PoseClip pose,
            PoseGroundingSuggestion suggestion)
        {
            var preset = UnityEngine.ScriptableObject.CreateInstance<AvatarAdjustmentPreset>();
            preset.avatarName = avatarName ?? "";
            if (pose == null || suggestion == null)
            {
                return preset;
            }

            preset.adjustments.Add(new PoseAdjustmentEntry
            {
                poseStableGuid = pose.StableGuid,
                rootOffset = new UnityEngine.Vector3(
                    pose.rootOffset.x,
                    suggestion.SuggestedRootYOffset,
                    pose.rootOffset.z),
                cameraOffset = suggestion.SuggestedCameraOffset
            });
            return preset;
        }
    }
}
