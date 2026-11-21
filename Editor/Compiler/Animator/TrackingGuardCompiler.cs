using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum TrackingGuardProfile
    {
        None,
        ThreePointOnly,
        ThreeToSix,
        FullBodyOnly
    }

    internal static class TrackingGuardCompiler
    {
        public static TrackingGuardProfile RootEntryProfile(PoseTuneRoot root)
        {
            if (root == null || !root.disableWhenFullBodyTracking)
            {
                return TrackingGuardProfile.None;
            }

            return root.advancedSettings.allowFullBodyTracking
                ? TrackingGuardProfile.ThreeToSix
                : TrackingGuardProfile.ThreePointOnly;
        }

        public static TrackingGuardProfile PoseEntryProfile(PoseTuneRoot root, PoseDefinition pose, bool fullBodyVariant)
        {
            if (pose != null && PoseStateVariantRules.NeedsFullBodyVariant(root, pose.Group))
            {
                return fullBodyVariant ? TrackingGuardProfile.FullBodyOnly : TrackingGuardProfile.ThreePointOnly;
            }

            return RootEntryProfile(root);
        }

        public static void AddEntryConditions(AnimatorStateTransition transition, TrackingGuardProfile profile)
        {
            switch (profile)
            {
                case TrackingGuardProfile.ThreePointOnly:
                    transition.AddCondition(AnimatorConditionMode.Greater, 2f, "TrackingType");
                    transition.AddCondition(AnimatorConditionMode.Less, 4f, "TrackingType");
                    break;
                case TrackingGuardProfile.ThreeToSix:
                    transition.AddCondition(AnimatorConditionMode.Greater, 2f, "TrackingType");
                    transition.AddCondition(AnimatorConditionMode.Less, 7f, "TrackingType");
                    break;
                case TrackingGuardProfile.FullBodyOnly:
                    transition.AddCondition(AnimatorConditionMode.Greater, 3f, "TrackingType");
                    transition.AddCondition(AnimatorConditionMode.Less, 7f, "TrackingType");
                    break;
            }
        }

        public static void AddInvalidExitTransitions(
            AnimatorState state,
            AnimatorState destination,
            TrackingGuardProfile profile)
        {
            switch (profile)
            {
                case TrackingGuardProfile.ThreePointOnly:
                    AddExit(state, destination, AnimatorConditionMode.Greater, 3f);
                    AddExit(state, destination, AnimatorConditionMode.Less, 3f);
                    break;
                case TrackingGuardProfile.ThreeToSix:
                    AddExit(state, destination, AnimatorConditionMode.Greater, 6f);
                    AddExit(state, destination, AnimatorConditionMode.Less, 3f);
                    break;
                case TrackingGuardProfile.FullBodyOnly:
                    AddExit(state, destination, AnimatorConditionMode.Greater, 6f);
                    AddExit(state, destination, AnimatorConditionMode.Less, 4f);
                    break;
            }
        }

        private static void AddExit(
            AnimatorState state,
            AnimatorState destination,
            AnimatorConditionMode mode,
            float threshold)
        {
            var transition = AnimatorTransitionFactory.Immediate(state, destination);
            transition.AddCondition(mode, threshold, "TrackingType");
        }
    }
}
