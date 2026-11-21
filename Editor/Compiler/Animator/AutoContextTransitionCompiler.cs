using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AutoContextTransitionCompiler
    {
        internal static bool ShouldResetManualPoseOnAutoMode(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            bool hasAutoEntry)
        {
            return root.enableAutoContextSwitch &&
                   group.ActivationMode == PoseGroupActivationMode.ManualAndAuto &&
                   !hasAutoEntry;
        }

        internal static bool AllowsManualEntry(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return !root.enableAutoContextSwitch || group.ActivationMode != PoseGroupActivationMode.Auto;
        }

        internal static bool AllowsAutoEntry(
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

        internal static void AddSelectedPoseAutoEntryCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseSelectionPlan poseSelection)
        {
            if (!UsesSelectedPoseAutoEntry(group))
            {
                return;
            }

            var binding = poseSelection.Find(pose);
            transition.AddCondition(AnimatorConditionMode.Equals, binding.Value, binding.ParameterName);
        }

        internal static void AddSelectedPoseAutoExitTransition(
            AnimatorState state,
            AnimatorState cleanup,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseSelectionPlan poseSelection)
        {
            if (!UsesSelectedPoseAutoEntry(group))
            {
                return;
            }

            var transition = AnimatorTransitionFactory.Immediate(state, cleanup);
            ModeTransitionCompiler.AddAutoModeCondition(transition, root);
            var binding = poseSelection.Find(pose);
            transition.AddCondition(AnimatorConditionMode.NotEqual, binding.Value, binding.ParameterName);
        }

        internal static List<ParameterConditionData> AutoContextConditions(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return PoseTuneAutoContextConditionService.AutoContextConditions(root, group);
        }

        internal static void AddAutoContextExitTransitions(
            AnimatorState state,
            AnimatorState reset,
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            foreach (var condition in AutoContextConditions(root, group))
            {
                var inverted = InvertAutoContextExitCondition(condition);
                var transition = AnimatorTransitionFactory.Immediate(state, reset);
                ModeTransitionCompiler.AddAutoModeCondition(transition, root);
                ConditionTransitionCompiler.AddConditions(transition, new[] { inverted });
            }
        }

        private static ParameterConditionData InvertAutoContextExitCondition(ParameterConditionData condition)
        {
            return PoseTuneConditionUtility.InvertForAutoContextExit(condition);
        }
    }
}
