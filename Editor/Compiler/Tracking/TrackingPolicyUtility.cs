using System;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class TrackingPolicyUtility
    {
        /// <summary>Every <see cref="TrackingPart"/>; each maps to one <see cref="TrackingPolicyData"/> field.</summary>
        private static readonly TrackingPart[] Parts = (TrackingPart[])Enum.GetValues(typeof(TrackingPart));

        /// <summary>The mode <paramref name="policy"/> sets for <paramref name="part"/>; a null policy changes nothing.</summary>
        public static TrackingMode Get(TrackingPolicyData policy, TrackingPart part)
        {
            policy ??= NoChange();
            switch (part)
            {
                case TrackingPart.Head:
                    return policy.head;
                case TrackingPart.LeftHand:
                    return policy.leftHand;
                case TrackingPart.RightHand:
                    return policy.rightHand;
                case TrackingPart.Hip:
                    return policy.hip;
                case TrackingPart.LeftFoot:
                    return policy.leftFoot;
                case TrackingPart.RightFoot:
                    return policy.rightFoot;
                case TrackingPart.LeftFingers:
                    return policy.leftFingers;
                case TrackingPart.RightFingers:
                    return policy.rightFingers;
                case TrackingPart.Eyes:
                    return policy.eyes;
                case TrackingPart.Mouth:
                    return policy.mouth;
                default:
                    return TrackingMode.NoChange;
            }
        }

        public static void Set(TrackingPolicyData policy, TrackingPart part, TrackingMode mode)
        {
            switch (part)
            {
                case TrackingPart.Head:
                    policy.head = mode;
                    break;
                case TrackingPart.LeftHand:
                    policy.leftHand = mode;
                    break;
                case TrackingPart.RightHand:
                    policy.rightHand = mode;
                    break;
                case TrackingPart.Hip:
                    policy.hip = mode;
                    break;
                case TrackingPart.LeftFoot:
                    policy.leftFoot = mode;
                    break;
                case TrackingPart.RightFoot:
                    policy.rightFoot = mode;
                    break;
                case TrackingPart.LeftFingers:
                    policy.leftFingers = mode;
                    break;
                case TrackingPart.RightFingers:
                    policy.rightFingers = mode;
                    break;
                case TrackingPart.Eyes:
                    policy.eyes = mode;
                    break;
                case TrackingPart.Mouth:
                    policy.mouth = mode;
                    break;
            }
        }

        public static TrackingPolicyData Copy(TrackingPolicyData source)
        {
            source ??= TrackingPolicyData.DefaultForPose();
            var copy = new TrackingPolicyData();
            foreach (var part in Parts)
            {
                Set(copy, part, Get(source, part));
            }

            return copy;
        }

        public static void ApplyResolved(
            PoseTrackingPolicy target,
            ResolvedGroupTrackingPolicy source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.tracking = Copy(source.Policy);
            target.useFullBodyTrackingOverride = source.HasFullBodyTrackingOverride;
            target.fullBodyTracking = Copy(source.FullBodyTrackingPolicy);
            target.generateResetOnExit = source.GenerateResetOnExit;
            target.enabled = true;
        }

        public static TrackingPolicyData NoChange()
        {
            var policy = new TrackingPolicyData();
            foreach (var part in Parts)
            {
                Set(policy, part, TrackingMode.NoChange);
            }

            return policy;
        }

        public static TrackingPolicyData DefaultForGroup(PoseGroupKind kind)
        {
            switch (kind)
            {
                case PoseGroupKind.Standing:
                    return new TrackingPolicyData
                    {
                        head = TrackingMode.Tracking,
                        leftHand = TrackingMode.Tracking,
                        rightHand = TrackingMode.Tracking,
                        hip = TrackingMode.NoChange,
                        leftFoot = TrackingMode.NoChange,
                        rightFoot = TrackingMode.NoChange
                    };
                case PoseGroupKind.Chair:
                case PoseGroupKind.Floor:
                    return new TrackingPolicyData
                    {
                        head = TrackingMode.Tracking,
                        leftHand = TrackingMode.Animation,
                        rightHand = TrackingMode.Animation,
                        hip = TrackingMode.Animation,
                        leftFoot = TrackingMode.Animation,
                        rightFoot = TrackingMode.Animation
                    };
                case PoseGroupKind.Prone:
                case PoseGroupKind.Supine:
                    return new TrackingPolicyData
                    {
                        head = TrackingMode.Animation,
                        leftHand = TrackingMode.Animation,
                        rightHand = TrackingMode.Animation,
                        hip = TrackingMode.Animation,
                        leftFoot = TrackingMode.Animation,
                        rightFoot = TrackingMode.Animation
                    };
                default:
                    return NoChange();
            }
        }

        public static bool AreEqual(TrackingPolicyData left, TrackingPolicyData right)
        {
            left ??= TrackingPolicyData.DefaultForPose();
            right ??= TrackingPolicyData.DefaultForPose();
            foreach (var part in Parts)
            {
                if (Get(left, part) != Get(right, part))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsNoChange(TrackingPolicyData value)
        {
            return value != null && AreEqual(value, NoChange());
        }
    }
}
