using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class TrackingPolicyData
    {
        [InspectorName("頭")]
        public TrackingMode head = TrackingMode.Tracking;
        [InspectorName("左手")]
        public TrackingMode leftHand = TrackingMode.Animation;
        [InspectorName("右手")]
        public TrackingMode rightHand = TrackingMode.Animation;
        [InspectorName("腰")]
        public TrackingMode hip = TrackingMode.Animation;
        [InspectorName("左足")]
        public TrackingMode leftFoot = TrackingMode.Animation;
        [InspectorName("右足")]
        public TrackingMode rightFoot = TrackingMode.Animation;
        [InspectorName("左指")]
        public TrackingMode leftFingers = TrackingMode.NoChange;
        [InspectorName("右指")]
        public TrackingMode rightFingers = TrackingMode.NoChange;
        [InspectorName("目")]
        public TrackingMode eyes = TrackingMode.NoChange;
        [InspectorName("口")]
        public TrackingMode mouth = TrackingMode.NoChange;

        public static TrackingPolicyData DefaultForPose()
        {
            return new TrackingPolicyData();
        }

        public static TrackingPolicyData ResetToTracking()
        {
            return new TrackingPolicyData
            {
                head = TrackingMode.Tracking,
                leftHand = TrackingMode.Tracking,
                rightHand = TrackingMode.Tracking,
                hip = TrackingMode.Tracking,
                leftFoot = TrackingMode.Tracking,
                rightFoot = TrackingMode.Tracking,
                leftFingers = TrackingMode.Tracking,
                rightFingers = TrackingMode.Tracking,
                eyes = TrackingMode.Tracking,
                mouth = TrackingMode.Tracking
            };
        }
    }
}
