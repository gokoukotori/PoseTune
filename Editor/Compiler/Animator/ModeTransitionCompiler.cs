using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ModeTransitionCompiler
    {
        internal static void AddManualModeEntryCondition(AnimatorStateTransition transition, PoseTuneRoot root)
        {
            transition.AddCondition(AnimatorConditionMode.Equals, PoseTuneModeValues.Manual, root.Parameter(PoseTuneNames.Mode));
        }

        internal static void AddAutoModeCondition(AnimatorStateTransition transition, PoseTuneRoot root)
        {
            transition.AddCondition(AnimatorConditionMode.Equals, PoseTuneModeValues.Auto, root.Parameter(PoseTuneNames.Mode));
        }

        internal static void AddDesktopModeCondition(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.Less, 1f, "VRMode");
        }

        internal static void AddVrModeCondition(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.Greater, 0f, "VRMode");
        }

        /// <summary>Leaves a desktop-only state once the user is in VR.</summary>
        internal static void AddDesktopModeInvalidExitTransition(AnimatorState state, AnimatorState destination)
        {
            var transition = AnimatorTransitionFactory.Immediate(state, destination);
            AddVrModeCondition(transition);
        }

        /// <summary>Leaves a VR-only state once the user is on desktop.</summary>
        internal static void AddVrModeInvalidExitTransition(AnimatorState state, AnimatorState destination)
        {
            var transition = AnimatorTransitionFactory.Immediate(state, destination);
            AddDesktopModeCondition(transition);
        }

        internal static void AddModeExitCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            var modeParameter = root.Parameter(PoseTuneNames.Mode);
            if (!root.enableAutoContextSwitch)
            {
                transition.AddCondition(AnimatorConditionMode.NotEqual, PoseTuneModeValues.Manual, modeParameter);
                return;
            }

            switch (group.ActivationMode)
            {
                case PoseGroupActivationMode.Manual:
                    transition.AddCondition(AnimatorConditionMode.NotEqual, PoseTuneModeValues.Manual, modeParameter);
                    break;
                case PoseGroupActivationMode.Auto:
                    transition.AddCondition(AnimatorConditionMode.NotEqual, PoseTuneModeValues.Auto, modeParameter);
                    break;
                default:
                    transition.AddCondition(AnimatorConditionMode.Equals, PoseTuneModeValues.Off, modeParameter);
                    break;
            }
        }
    }
}
