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
            policy ??= TrackingPolicyUtility.NoChange();
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

        public static void AddTrackingBehavior(AnimatorState state, TrackingPart part, TrackingMode mode)
        {
            var policy = TrackingPolicyUtility.NoChange();
            SetMode(policy, part, mode);
            AddTrackingBehavior(state, policy);
        }

        public static TrackingMode Mode(TrackingPolicyData policy, TrackingPart part)
        {
            policy ??= TrackingPolicyUtility.NoChange();
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

        private static void SetMode(TrackingPolicyData policy, TrackingPart part, TrackingMode mode)
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
