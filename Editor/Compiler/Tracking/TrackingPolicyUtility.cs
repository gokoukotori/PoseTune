namespace Gokoukotori.PoseTune.Editor
{
    internal static class TrackingPolicyUtility
    {
        public static TrackingPolicyData Copy(TrackingPolicyData source)
        {
            source ??= TrackingPolicyData.DefaultForPose();
            return new TrackingPolicyData
            {
                head = source.head,
                leftHand = source.leftHand,
                rightHand = source.rightHand,
                hip = source.hip,
                leftFoot = source.leftFoot,
                rightFoot = source.rightFoot,
                leftFingers = source.leftFingers,
                rightFingers = source.rightFingers,
                eyes = source.eyes,
                mouth = source.mouth
            };
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
            return new TrackingPolicyData
            {
                head = TrackingMode.NoChange,
                leftHand = TrackingMode.NoChange,
                rightHand = TrackingMode.NoChange,
                hip = TrackingMode.NoChange,
                leftFoot = TrackingMode.NoChange,
                rightFoot = TrackingMode.NoChange,
                leftFingers = TrackingMode.NoChange,
                rightFingers = TrackingMode.NoChange,
                eyes = TrackingMode.NoChange,
                mouth = TrackingMode.NoChange
            };
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
            return left.head == right.head &&
                   left.leftHand == right.leftHand &&
                   left.rightHand == right.rightHand &&
                   left.hip == right.hip &&
                   left.leftFoot == right.leftFoot &&
                   left.rightFoot == right.rightFoot &&
                   left.leftFingers == right.leftFingers &&
                   left.rightFingers == right.rightFingers &&
                   left.eyes == right.eyes &&
                   left.mouth == right.mouth;
        }

        public static bool IsNoChange(TrackingPolicyData value)
        {
            return value != null && AreEqual(value, NoChange());
        }

        public static bool WasCustomizedFromPoseDefault(TrackingPolicyData value)
        {
            return !AreEqual(value, TrackingPolicyData.DefaultForPose());
        }
    }
}
