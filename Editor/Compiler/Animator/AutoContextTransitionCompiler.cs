using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static bool ShouldResetManualPoseOnAutoMode(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            bool hasAutoEntry)
        {
            return root.enableAutoContextSwitch &&
                   group.ActivationMode == PoseGroupActivationMode.ManualAndAuto &&
                   !hasAutoEntry;
        }

        private static bool AllowsManualEntry(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return !root.enableAutoContextSwitch || group.ActivationMode != PoseGroupActivationMode.Auto;
        }

        private static bool AllowsAutoEntry(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose,
            List<ParameterConditionData> branch)
        {
            if (!root.enableAutoContextSwitch ||
                group.ActivationMode == PoseGroupActivationMode.Manual)
            {
                return false;
            }

            return group.Kind != PoseGroupKind.Custom || (branch != null && branch.Count > 0);
        }

        private static bool UsesSelectedPoseAutoEntry(PoseGroupDefinition group)
        {
            return group != null && group.AutoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup;
        }

        private static void AddSelectedPoseAutoEntryCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            if (!UsesSelectedPoseAutoEntry(group))
            {
                return;
            }

            transition.AddCondition(AnimatorConditionMode.Equals, pose.SelectionValue(root), group.ParameterName);
        }

        private static void AddSelectedPoseAutoExitTransition(
            AnimatorState state,
            AnimatorState cleanup,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            if (!UsesSelectedPoseAutoEntry(group))
            {
                return;
            }

            var transition = state.AddTransition(cleanup);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.Equals, 1, root.Parameter(PoseTuneNames.Mode));
            transition.AddCondition(AnimatorConditionMode.NotEqual, pose.SelectionValue(root), group.ParameterName);
        }

        private static List<ParameterConditionData> AutoContextConditions(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return PoseTuneAutoContextConditionService.AutoContextConditions(root, group);
        }

        private static void AddAutoContextExitTransitions(
            AnimatorState state,
            AnimatorState reset,
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            foreach (var condition in AutoContextConditions(root, group))
            {
                var inverted = InvertAutoContextExitCondition(condition);
                var transition = state.AddTransition(reset);
                transition.hasExitTime = false;
                transition.duration = 0f;
                transition.AddCondition(AnimatorConditionMode.Equals, 1, root.Parameter(PoseTuneNames.Mode));
                AddConditions(transition, new[] { inverted });
            }
        }

        private static ParameterConditionData InvertAutoContextExitCondition(ParameterConditionData condition)
        {
            return PoseTuneConditionUtility.InvertForAutoContextExit(condition);
        }
    }
}
