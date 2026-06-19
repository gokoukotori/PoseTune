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
            var policy = TrackingPolicyData.DefaultForPose();
            if (state == null)
            {
                return policy;
            }

            foreach (var behavior in state.behaviours.Where(IsTrackingBehavior))
            {
                policy.head = ReadTrackingField(behavior, "trackingHead", policy.head);
                policy.leftHand = ReadTrackingField(behavior, "trackingLeftHand", policy.leftHand);
                policy.rightHand = ReadTrackingField(behavior, "trackingRightHand", policy.rightHand);
                policy.hip = ReadTrackingField(behavior, "trackingHip", policy.hip);
                policy.leftFoot = ReadTrackingField(behavior, "trackingLeftFoot", policy.leftFoot);
                policy.rightFoot = ReadTrackingField(behavior, "trackingRightFoot", policy.rightFoot);
                policy.leftFingers = ReadTrackingField(behavior, "trackingLeftFingers", policy.leftFingers);
                policy.rightFingers = ReadTrackingField(behavior, "trackingRightFingers", policy.rightFingers);
                policy.eyes = ReadTrackingField(behavior, "trackingEyes", policy.eyes);
                policy.mouth = ReadTrackingField(behavior, "trackingMouth", policy.mouth);
            }

            return policy;
        }

        private static bool IsTrackingBehavior(StateMachineBehaviour behavior)
        {
            return behavior != null && behavior.GetType().Name.Contains("AnimatorTrackingControl");
        }

        private static TrackingMode ReadTrackingField(
            StateMachineBehaviour behavior,
            string fieldName,
            TrackingMode fallback)
        {
            var field = behavior.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                return fallback;
            }

            var value = field.GetValue(behavior)?.ToString();
            switch (value)
            {
                case "Tracking":
                    return TrackingMode.Tracking;
                case "Animation":
                    return TrackingMode.Animation;
                default:
                    return TrackingMode.NoChange;
            }
        }
    }
}
