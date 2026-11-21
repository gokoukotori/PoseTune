using System.Collections.Generic;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateVariantRules
    {
        /// <summary>
        /// The state variants a pose compiles to, in creation order (Base, Desktop, VR, FBT), with their static
        /// facts and layout. Pure: the compiler fills in tracking policies, votes and states.
        /// </summary>
        public static PoseStateVariants Plan(PoseTuneRoot root, PoseGroupDefinition group, PoseDefinition pose)
        {
            var needsPoseSpaceVrVariant = NeedsPoseSpaceVrVariant(pose);
            var needsDesktopVariant = NeedsDesktopVariant(root, group, pose);
            var all = new List<PoseStateVariant>
            {
                Variant(PoseStateVariantKind.Base, "", pose, 0, 1120, 280)
            };
            // Desktop and VR variants never coexist, so their commit states share a column.
            if (needsDesktopVariant)
            {
                all.Add(Variant(PoseStateVariantKind.Desktop, PoseStateNaming.DesktopSuffix, pose, 280, 1260, 420));
            }

            if (needsPoseSpaceVrVariant)
            {
                all.Add(Variant(
                    PoseStateVariantKind.Vr,
                    PoseStateNaming.VrSuffix,
                    pose,
                    needsDesktopVariant ? 560 : 280,
                    1400,
                    420));
            }

            if (NeedsFullBodyVariant(root, group))
            {
                all.Add(Variant(
                    PoseStateVariantKind.FullBody,
                    PoseStateNaming.FullBodySuffix,
                    pose,
                    needsDesktopVariant ? 840 : 560,
                    1540,
                    560));
            }

            return new PoseStateVariants(all, needsPoseSpaceVrVariant, needsDesktopVariant);
        }

        /// <summary>The group overrides tracking for full body tracking, so its poses get an "_FBT" state.</summary>
        public static bool NeedsFullBodyVariant(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return group != null &&
                   group.HasFullBodyTrackingOverride &&
                   root?.advancedSettings?.allowFullBodyTracking == true;
        }

        /// <summary>
        /// Whether a variant's states apply the pose's pose space settings. The VR variant of a DesktopOnly pose
        /// space plays without it, and so does the FBT variant then.
        /// </summary>
        public static bool AppliesPoseSpace(PoseStateVariantKind kind, PoseDefinition pose)
        {
            switch (kind)
            {
                case PoseStateVariantKind.Base:
                    return true;
                case PoseStateVariantKind.Desktop:
                    return DesktopVariantEntersPoseSpace(pose);
                case PoseStateVariantKind.Vr:
                    return false;
                default:
                    return !NeedsPoseSpaceVrVariant(pose);
            }
        }

        /// <summary>DesktopOnly pose space: the base state is desktop and a "_VR" state plays without it.</summary>
        public static bool NeedsPoseSpaceVrVariant(PoseDefinition pose)
        {
            return pose?.PoseSpace != null &&
                   pose.PoseSpace.enabled &&
                   pose.PoseSpace.scope == PoseSpaceScope.DesktopOnly;
        }

        /// <summary>
        /// The base state is VR and a "_Desktop" state plays on desktop. It is needed to lock the desktop lower
        /// body, or when pose space is VR only so that desktop plays the pose without it.
        /// </summary>
        public static bool NeedsDesktopVariant(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return NeedsDesktopLowerBodyLockVariant(root, group, pose) || PoseSpaceIsVrOnly(pose);
        }

        /// <summary>The "_Desktop" state uses <see cref="DesktopLowerBodyTrackingPolicy"/>.</summary>
        public static bool NeedsDesktopLowerBodyLockVariant(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return ShouldLockDesktopLowerBodyTracking(root, group, pose) &&
                   !NeedsPoseSpaceVrVariant(pose);
        }

        public static bool DesktopVariantEntersPoseSpace(PoseDefinition pose)
        {
            return !PoseSpaceIsVrOnly(pose);
        }

        /// <summary>
        /// Whether a variant that applies the pose's policy actually enters pose space, so its cleanup must
        /// leave it. Pose space is global per avatar; leaving it from other poses would cancel another
        /// group's or another gimmick's pose space.
        /// </summary>
        public static bool EntersPoseSpace(PoseDefinition pose, bool variantAppliesPoseSpace)
        {
            return variantAppliesPoseSpace &&
                   pose?.PoseSpace != null &&
                   pose.PoseSpace.enabled &&
                   pose.PoseSpace.enterPoseSpace;
        }

        public static bool LocksExistingDesktopPoseState(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return ShouldLockDesktopLowerBodyTracking(root, group, pose) &&
                   NeedsPoseSpaceVrVariant(pose);
        }

        public static TrackingPolicyData DesktopVariantTrackingPolicy(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            return NeedsDesktopLowerBodyLockVariant(root, group, pose)
                ? DesktopLowerBodyTrackingPolicy(group.TrackingPolicy)
                : TrackingPolicyUtility.Copy(group.TrackingPolicy);
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
                   !LowerBodyAlreadyAnimation(group.TrackingPolicy);
        }

        private static PoseStateVariant Variant(
            PoseStateVariantKind kind,
            string suffix,
            PoseDefinition pose,
            int stateX,
            int handoffX,
            int commitX)
        {
            return new PoseStateVariant
            {
                Kind = kind,
                Suffix = suffix,
                AppliesPoseSpace = AppliesPoseSpace(kind, pose),
                StateX = stateX,
                HandoffX = handoffX,
                CommitX = commitX
            };
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
