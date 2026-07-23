using System.Linq;
using System.Reflection;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorTrackingPolicyReader
    {
        public static bool HasTrackingBehavior(AnimatorState state)
        {
            return state != null && state.behaviours.Any(IsTrackingBehavior);
        }

        public static TrackingPolicyData ReadTrackingPolicy(AnimatorState state)
        {
            var policy = TrackingPolicyUtility.NoChange();
            if (state == null)
            {
                return policy;
            }

            foreach (var behavior in state.behaviours.Where(IsTrackingBehavior))
            {
                policy.head = MergeTrackingField(policy.head, behavior, "trackingHead");
                policy.leftHand = MergeTrackingField(policy.leftHand, behavior, "trackingLeftHand");
                policy.rightHand = MergeTrackingField(policy.rightHand, behavior, "trackingRightHand");
                policy.hip = MergeTrackingField(policy.hip, behavior, "trackingHip");
                policy.leftFoot = MergeTrackingField(policy.leftFoot, behavior, "trackingLeftFoot");
                policy.rightFoot = MergeTrackingField(policy.rightFoot, behavior, "trackingRightFoot");
                policy.leftFingers = MergeTrackingField(policy.leftFingers, behavior, "trackingLeftFingers");
                policy.rightFingers = MergeTrackingField(policy.rightFingers, behavior, "trackingRightFingers");
                policy.eyes = MergeTrackingField(policy.eyes, behavior, "trackingEyes");
                policy.mouth = MergeTrackingField(policy.mouth, behavior, "trackingMouth");
            }

            return policy;
        }

        private static bool IsTrackingBehavior(StateMachineBehaviour behavior)
        {
            return behavior != null && behavior.GetType().Name.Contains("AnimatorTrackingControl");
        }

        private static TrackingMode MergeTrackingField(
            TrackingMode current,
            StateMachineBehaviour behavior,
            string fieldName)
        {
            var field = behavior.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                return current;
            }

            var value = field.GetValue(behavior)?.ToString();
            switch (value)
            {
                case "Tracking":
                    return TrackingMode.Tracking;
                case "Animation":
                    return TrackingMode.Animation;
                default:
                    return current;
            }
        }
    }
}
