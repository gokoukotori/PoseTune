using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void AddManualModeEntryCondition(AnimatorStateTransition transition, PoseTuneRoot root)
        {
            transition.AddCondition(AnimatorConditionMode.Equals, 2, root.Parameter(PoseTuneNames.Mode));
        }

        private static void AddDesktopModeCondition(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.Less, 1f, "VRMode");
        }

        private static void AddVrModeCondition(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.Greater, 0f, "VRMode");
        }

        private static void AddDesktopModeInvalidExitTransition(AnimatorState state, AnimatorState destination)
        {
            var transition = state.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.Greater, 0f, "VRMode");
        }

        private static void AddVrModeInvalidExitTransition(AnimatorState state, AnimatorState destination)
        {
            var transition = state.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.Less, 1f, "VRMode");
        }

        private static void AddModeExitCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            var modeParameter = root.Parameter(PoseTuneNames.Mode);
            if (!root.enableAutoContextSwitch)
            {
                transition.AddCondition(AnimatorConditionMode.NotEqual, 2, modeParameter);
                return;
            }

            switch (group.ActivationMode)
            {
                case PoseGroupActivationMode.Manual:
                    transition.AddCondition(AnimatorConditionMode.NotEqual, 2, modeParameter);
                    break;
                case PoseGroupActivationMode.Auto:
                    transition.AddCondition(AnimatorConditionMode.NotEqual, 1, modeParameter);
                    break;
                default:
                    transition.AddCondition(AnimatorConditionMode.Equals, 0, modeParameter);
                    break;
            }
        }
    }
}
