using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseEntryTransitionBuilder
    {
        internal static bool AddPoseEntryTransitions(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariants variants,
            string poseActiveParameter,
            int x,
            int y)
        {
            var root = context.Root;
            var group = context.Group;
            var selectionBinding = context.PoseSelection.Find(pose);
            var conditionBranches = ConditionTransitionCompiler.EffectiveConditionBranches(pose);
            var hasAutoEntry = conditionBranches.Any(branch =>
                AutoContextTransitionCompiler.AllowsAutoEntry(root, group, pose, branch));
            foreach (var branch in conditionBranches)
            {
                // Within a branch every manual entry precedes every auto entry; any-state order is priority.
                if (AutoContextTransitionCompiler.AllowsManualEntry(root, group))
                {
                    foreach (var variant in variants.All)
                    {
                        AddManualEntry(
                            context,
                            pose,
                            variants,
                            variant,
                            branch,
                            selectionBinding,
                            poseActiveParameter,
                            x,
                            y);
                    }
                }

                if (AutoContextTransitionCompiler.AllowsAutoEntry(root, group, pose, branch))
                {
                    foreach (var variant in variants.All)
                    {
                        AddAutoEntry(context, pose, variants, variant, branch);
                    }
                }
            }

            return hasAutoEntry;
        }

        private static void AddManualEntry(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariants variants,
            PoseStateVariant variant,
            List<ParameterConditionData> branch,
            PoseSelectionBinding selectionBinding,
            string poseActiveParameter,
            int x,
            int y)
        {
            var target = variant.State;
            if (context.Group.Exclusive && context.ExclusiveResetTargets.Count > 0)
            {
                variant.Commit ??= CreateExclusiveCommitState(context, pose, variant, x, y);
                target = variant.Commit;
            }

            var enter = AnimatorTransitionFactory.ImmediateFromAny(context.Layer.stateMachine, target);
            ModeTransitionCompiler.AddManualModeEntryCondition(enter, context.Root);
            AddTrackingVoteClearedCondition(enter, context.Graph, context.Group);
            enter.AddCondition(
                AnimatorConditionMode.Equals,
                selectionBinding.Value,
                selectionBinding.ParameterName);
            AddManualCommitReentryGuard(enter, target, variant.State, poseActiveParameter);
            AddFbtEntryCondition(enter, context.Root, pose, variant.IsFullBody);
            AddVariantScopeCondition(enter, pose, variants, variant);
            ConditionTransitionCompiler.AddConditions(enter, branch);
        }

        private static void AddAutoEntry(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariants variants,
            PoseStateVariant variant,
            List<ParameterConditionData> branch)
        {
            var enter = AnimatorTransitionFactory.ImmediateFromAny(context.Layer.stateMachine, variant.State);
            ModeTransitionCompiler.AddAutoModeCondition(enter, context.Root);
            AddTrackingVoteClearedCondition(enter, context.Graph, context.Group);
            AutoContextTransitionCompiler.AddSelectedPoseAutoEntryCondition(
                enter,
                context.Root,
                context.Group,
                pose,
                context.PoseSelection);
            AddFbtEntryCondition(enter, context.Root, pose, variant.IsFullBody);
            AddVariantScopeCondition(enter, pose, variants, variant);
            ConditionTransitionCompiler.AddConditions(
                enter,
                AutoContextTransitionCompiler.AutoContextConditions(context.Root, context.Group));
            ConditionTransitionCompiler.AddConditions(enter, branch);
        }

        /// <summary>Restricts an entry to the mode or pose space scope its variant plays in.</summary>
        private static void AddVariantScopeCondition(
            AnimatorStateTransition transition,
            PoseDefinition pose,
            PoseStateVariants variants,
            PoseStateVariant variant)
        {
            switch (variant.Kind)
            {
                case PoseStateVariantKind.Base:
                    if (variants.NeedsDesktopVariant)
                    {
                        ModeTransitionCompiler.AddVrModeCondition(transition);
                    }
                    else
                    {
                        PoseSpaceTransitionCompiler.AddPoseSpaceScopeCondition(transition, pose);
                    }

                    break;
                case PoseStateVariantKind.Desktop:
                    ModeTransitionCompiler.AddDesktopModeCondition(transition);
                    break;
                case PoseStateVariantKind.Vr:
                    PoseSpaceTransitionCompiler.AddPoseSpaceScopeCondition(transition, pose, true);
                    break;
                default:
                    PoseSpaceTransitionCompiler.AddPoseSpaceScopeCondition(
                        transition,
                        pose,
                        variants.NeedsPoseSpaceVrVariant);
                    break;
            }
        }

        private static void AddFbtEntryCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseDefinition pose,
            bool fullBodyVariant)
        {
            TrackingGuardCompiler.AddEntryConditions(
                transition,
                TrackingGuardCompiler.PoseEntryProfile(root, pose, fullBodyVariant));
        }

        private static void AddManualCommitReentryGuard(
            AnimatorStateTransition transition,
            AnimatorState manualTarget,
            AnimatorState poseState,
            string poseActiveParameter)
        {
            if (manualTarget == poseState || string.IsNullOrWhiteSpace(poseActiveParameter))
            {
                return;
            }

            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, poseActiveParameter);
        }

        private static void AddTrackingVoteClearedCondition(
            AnimatorStateTransition transition,
            PoseGraph graph,
            PoseGroupDefinition group)
        {
            if (ParameterAllocator.RequiresTrackingVote(graph, group))
            {
                transition.AddCondition(
                    AnimatorConditionMode.Equals,
                    0f,
                    PoseTuneNames.TrackingVoteParameter(group));
            }

            foreach (var activeParameter in PoseTuneLayerNaming.GroupActiveParameters(group))
            {
                ActiveParameterConditions.AddInactiveCondition(transition, activeParameter);
            }
        }

        private static AnimatorState CreateExclusiveCommitState(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariant variant,
            int x,
            int y)
        {
            var destination = variant.State;
            var commit = context.Layer.stateMachine.AddState(
                PoseStateNaming.CommitExclusivePrefix + PoseStateNaming.Name(pose, context.DuplicateStateBaseNames) +
                variant.Suffix,
                new Vector3(x + variant.CommitX, y));
            CopyPoseStateSurface(destination, commit);
            ParameterDriverCompiler.ResetExclusiveParameters(
                commit,
                context.ExclusiveResetTargets,
                context.ResetLocalOnly);
            ParameterDriverCompiler.ResetPoseActiveParameters(commit, context.PoseActiveParameters);
            if (variant.AppliesPoseSpace)
            {
                PoseSpaceCompiler.AddEnterPoseSpaceBehavior(commit, pose.PoseSpace);
            }
            ParameterDriverCompiler.SetGroupActive(commit, context.ActiveParameter, 1f);
            if (variant.VoteId > 0)
            {
                ParameterDriverCompiler.SetTrackingVote(commit, context.Group, variant.VoteId);
            }
            AnimatorTransitionFactory.AfterExitTime(commit, destination, 0f, CommitStateHoldSeconds(pose));
            return commit;
        }

        private static float CommitStateHoldSeconds(PoseDefinition pose)
        {
            var poseSpaceDelay = pose?.PoseSpace != null && pose.PoseSpace.enabled && pose.PoseSpace.fixedDelay
                ? pose.PoseSpace.delayTime
                : 0f;
            return Mathf.Max(AnimatorCompiler.CriticalStateHoldSeconds, poseSpaceDelay);
        }

        private static void CopyPoseStateSurface(AnimatorState source, AnimatorState destination)
        {
            destination.motion = source.motion;
            destination.writeDefaultValues = source.writeDefaultValues;
            destination.timeParameterActive = source.timeParameterActive;
            destination.timeParameter = source.timeParameter;
        }
    }
}
