using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class TrackingCompiler
    {
        public static void AddTrackingBehavior(AnimatorState state, TrackingPolicyData policy)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            behavior.trackingHead = ToVrc(policy.head);
            behavior.trackingLeftHand = ToVrc(policy.leftHand);
            behavior.trackingRightHand = ToVrc(policy.rightHand);
            behavior.trackingHip = ToVrc(policy.hip);
            behavior.trackingLeftFoot = ToVrc(policy.leftFoot);
            behavior.trackingRightFoot = ToVrc(policy.rightFoot);
            behavior.trackingLeftFingers = ToVrc(policy.leftFingers);
            behavior.trackingRightFingers = ToVrc(policy.rightFingers);
            behavior.trackingEyes = ToVrc(policy.eyes);
            behavior.trackingMouth = ToVrc(policy.mouth);
        }

        private static VRC_AnimatorTrackingControl.TrackingType ToVrc(TrackingMode mode)
        {
            switch (mode)
            {
                case TrackingMode.Tracking:
                    return VRC_AnimatorTrackingControl.TrackingType.Tracking;
                case TrackingMode.Animation:
                    return VRC_AnimatorTrackingControl.TrackingType.Animation;
                default:
                    return VRC_AnimatorTrackingControl.TrackingType.NoChange;
            }
        }
    }
}
