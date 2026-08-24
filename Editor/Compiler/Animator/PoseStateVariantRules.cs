using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateVariantRules
    {
        public static bool NeedsPoseSpaceVrVariant(PoseDefinition pose)
        {
            return pose?.PoseSpace != null &&
                   pose.PoseSpace.enabled &&
                   pose.PoseSpace.scope == PoseSpaceScope.DesktopOnly;
        }

        public static bool NeedsDesktopLowerBodyLockVariant(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return ShouldLockDesktopLowerBodyTracking(root, group, pose) &&
                   !NeedsPoseSpaceVrVariant(pose);
        }

        public static bool LocksExistingDesktopPoseState(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return ShouldLockDesktopLowerBodyTracking(root, group, pose) &&
                   NeedsPoseSpaceVrVariant(pose);
        }

        public static TrackingPolicyData DesktopLowerBodyTrackingPolicy(TrackingPolicyData source)
        {
            var policy = TrackingPolicyUtility.Copy(source);
            policy.hip = TrackingMode.Animation;
            policy.leftFoot = TrackingMode.Animation;
            policy.rightFoot = TrackingMode.Animation;
            return policy;
        }

        private static bool ShouldLockDesktopLowerBodyTracking(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return root?.advancedSettings != null &&
                   root.advancedSettings.lockDesktopLowerBodyTracking &&
                   group != null &&
                   group.Kind == PoseGroupKind.Standing &&
                   pose != null &&
                   group.EmitTrackingControl &&
                   !PoseSpaceIsVrOnly(pose) &&
                   !LowerBodyAlreadyAnimation(group.TrackingPolicy);
        }

        private static bool PoseSpaceIsVrOnly(PoseDefinition pose)
        {
            return pose?.PoseSpace != null &&
                   pose.PoseSpace.enabled &&
                   pose.PoseSpace.scope == PoseSpaceScope.VROnly;
        }

        private static bool LowerBodyAlreadyAnimation(TrackingPolicyData policy)
        {
            policy ??= TrackingPolicyData.DefaultForPose();
            return policy.hip == TrackingMode.Animation &&
                   policy.leftFoot == TrackingMode.Animation &&
                   policy.rightFoot == TrackingMode.Animation;
        }
    }
}
