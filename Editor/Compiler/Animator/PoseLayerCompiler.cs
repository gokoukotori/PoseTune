using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void CreateActionPoseLayer(
            AnimatorBuildResult result,
            PoseGraph graph,
            PoseGroupDefinition group,
            List<PoseDefinition> poses,
            string layerName,
            PoseClipBlendMode blendMode,
            bool controlsActionPlayable,
            string activeParameter)
        {
            var layer = AnimatorLayerFactory.NewLayer(layerName);
            layer.blendingMode = blendMode == PoseClipBlendMode.Additive
                ? AnimatorLayerBlendingMode.Additive
                : AnimatorLayerBlendingMode.Override;
            var idle = layer.stateMachine.AddState("PassThrough", new Vector3(240, 80));
            var empty = AnimatorLayerFactory.EmptyClip("PT_Empty");
            idle.motion = empty;
            result.GeneratedAssets.Add(empty);
            layer.stateMachine.defaultState = idle;

            var exclusiveResetTargets = ExclusiveResetTargets(graph, group);
            var poseActiveParameters = NeedsManualCommitGuard(graph.RootComponent, group, exclusiveResetTargets)
                ? poses.Select(PoseTuneNames.PoseActiveParameter).Distinct().ToList()
                : new List<string>();
            var x = 240;
            var y = 180;
            var orderedPoses = PosesForTransitionGeneration(poses);
            var duplicateStateBaseNames = PoseStateNaming.DuplicateBaseNames(orderedPoses);
            foreach (var pose in orderedPoses)
            {
                var poseActiveParameter = poseActiveParameters.Contains(PoseTuneNames.PoseActiveParameter(pose))
                    ? PoseTuneNames.PoseActiveParameter(pose)
                    : "";
                var variants = PoseStateFactory.CreateVariants(
                    result,
                    layer,
                    graph,
                    group,
                    pose,
                    duplicateStateBaseNames,
                    new Vector3(x, y),
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter);
                variants.BaseHandoff = CreateCleanupState(
                    result,
                    layer,
                    graph,
                    group,
                    pose,
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames),
                    layerName + "_" + PoseTuneNames.ShortGuid(pose.Id) + "_HandoffHold",
                    new Vector3(x + 1120, y),
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameters,
                    variants.BaseTrackingPolicy);
                if (variants.DesktopLowerBodyState != null)
                {
                    variants.DesktopLowerBodyHandoff = CreateCleanupState(
                        result, layer, graph, group, pose,
                        PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_Desktop"),
                        layerName + "_" + PoseTuneNames.ShortGuid(pose.Id) + "_Desktop_HandoffHold",
                        new Vector3(x + 1260, y), controlsActionPlayable, activeParameter,
                        poseActiveParameters, variants.DesktopLowerBodyTrackingPolicy);
                }

                if (variants.VrState != null)
                {
                    variants.VrHandoff = CreateCleanupState(
                        result, layer, graph, group, pose,
                        PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_VR"),
                        layerName + "_" + PoseTuneNames.ShortGuid(pose.Id) + "_VR_HandoffHold",
                        new Vector3(x + 1400, y), controlsActionPlayable, activeParameter,
                        poseActiveParameters, variants.VrTrackingPolicy);
                }

                if (variants.FullBodyState != null)
                {
                    variants.FullBodyHandoff = CreateCleanupState(
                        result, layer, graph, group, pose,
                        PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_FBT"),
                        layerName + "_" + PoseTuneNames.ShortGuid(pose.Id) + "_FBT_HandoffHold",
                        new Vector3(x + 1540, y), controlsActionPlayable, activeParameter,
                        poseActiveParameters, variants.FullBodyTrackingPolicy);
                }
                var hasAutoEntry = AddPoseEntryTransitions(
                    layer,
                    graph,
                    group,
                    pose,
                    variants,
                    duplicateStateBaseNames,
                    exclusiveResetTargets,
                    poseActiveParameters,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    x,
                    y);
                AddPoseExitTransitions(variants, graph, group, pose, hasAutoEntry);
                AddCleanupReturnTransition(idle, variants.BaseHandoff);
                if (variants.DesktopLowerBodyHandoff != null)
                {
                    AddCleanupReturnTransition(idle, variants.DesktopLowerBodyHandoff);
                }
                if (variants.VrHandoff != null)
                {
                    AddCleanupReturnTransition(idle, variants.VrHandoff);
                }
                if (variants.FullBodyHandoff != null)
                {
                    AddCleanupReturnTransition(idle, variants.FullBodyHandoff);
                }

                y += 100;
            }

            result.TargetController.AddLayer(layer);
        }

        private static void AddHigherPriorityAutoPreemptionTransitions(
            AnimatorController controller,
            PoseGraph graph,
            PoseGroupDefinition group)
        {
            if (graph?.RootComponent == null ||
                !graph.RootComponent.enableAutoContextSwitch ||
                group == null ||
                group.ActivationMode == PoseGroupActivationMode.Manual)
            {
                return;
            }

            var records = AutoPreemptionRecords(controller, graph, group);
            var modeParameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var voteParameter = PoseTuneNames.TrackingVoteParameter(group);
            var activeParameters = new HashSet<string>(PoseTuneLayerNaming.GroupActiveParameters(group));
            for (var lowerIndex = 1; lowerIndex < records.Count; lowerIndex++)
            {
                var higherRecords = records.Take(lowerIndex).ToList();
                var higherStates = new HashSet<AnimatorState>(higherRecords
                    .SelectMany(record => record.Variants)
                    .Select(pair => pair.State));
                var higherAutoEntries = records
                    .Take(lowerIndex)
                    .SelectMany(record => record.Layer.stateMachine.anyStateTransitions ??
                        System.Array.Empty<AnimatorStateTransition>())
                    .Distinct()
                    .Where(transition =>
                        transition != null &&
                        transition.destinationState != null &&
                        higherStates.Contains(transition.destinationState) &&
                        (transition.conditions ?? System.Array.Empty<AnimatorCondition>()).Any(condition =>
                            condition.parameter == modeParameter &&
                            condition.mode == AnimatorConditionMode.Equals &&
                            Mathf.Approximately(condition.threshold, 1f)))
                    .ToList();

                foreach (var pair in records[lowerIndex].Variants)
                {
                    foreach (var higherEntry in higherAutoEntries)
                    {
                        var preempt = pair.State.AddTransition(pair.Handoff);
                        preempt.hasExitTime = false;
                        preempt.duration = 0f;
                        foreach (var condition in higherEntry.conditions ??
                                 System.Array.Empty<AnimatorCondition>())
                        {
                            if (condition.parameter == voteParameter &&
                                condition.mode == AnimatorConditionMode.Equals &&
                                Mathf.Approximately(condition.threshold, 0f))
                            {
                                continue;
                            }

                            if (activeParameters.Contains(condition.parameter) &&
                                condition.mode == AnimatorConditionMode.Less &&
                                Mathf.Approximately(condition.threshold, 0.5f))
                            {
                                continue;
                            }

                            preempt.AddCondition(condition.mode, condition.threshold, condition.parameter);
                        }
                    }
                }
            }
        }

        private static List<PoseLayerRecord> AutoPreemptionRecords(
            AnimatorController controller,
            PoseGraph graph,
            PoseGroupDefinition group)
        {
            var buckets = PoseTuneLayerNaming.LayerBuckets(group);
            var result = new List<PoseLayerRecord>();
            foreach (var pose in PosesForTransitionGeneration(group.Poses))
            {
                var bucket = buckets.First(candidate => candidate.Poses.Contains(pose));
                var layer = controller.layers.Last(candidate => candidate.name == bucket.LayerName);
                var duplicates = PoseStateNaming.DuplicateBaseNames(bucket.Poses);
                var variants = new List<PoseVariantHandoff>();
                AddVariantHandoff(
                    layer,
                    variants,
                    PoseStateNaming.Name(pose, duplicates),
                    PoseStateNaming.CleanupName(pose, duplicates));
                if (PoseStateVariantRules.NeedsDesktopLowerBodyLockVariant(graph.RootComponent, group, pose))
                {
                    AddVariantHandoff(
                        layer,
                        variants,
                        PoseStateNaming.Name(pose, duplicates, "_Desktop"),
                        PoseStateNaming.CleanupName(pose, duplicates, "_Desktop"));
                }

                if (PoseStateVariantRules.NeedsPoseSpaceVrVariant(pose))
                {
                    AddVariantHandoff(
                        layer,
                        variants,
                        PoseStateNaming.Name(pose, duplicates, "_VR"),
                        PoseStateNaming.CleanupName(pose, duplicates, "_VR"));
                }

                if (pose.HasFullBodyTrackingOverride &&
                    graph.RootComponent.advancedSettings?.allowFullBodyTracking == true)
                {
                    AddVariantHandoff(
                        layer,
                        variants,
                        PoseStateNaming.Name(pose, duplicates, "_FBT"),
                        PoseStateNaming.CleanupName(pose, duplicates, "_FBT"));
                }

                result.Add(new PoseLayerRecord
                {
                    Layer = layer,
                    Variants = variants
                });
            }

            return result;
        }

        private static void AddVariantHandoff(
            AnimatorControllerLayer layer,
            ICollection<PoseVariantHandoff> variants,
            string stateName,
            string handoffName)
        {
            var state = layer.stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
            var handoff = layer.stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == handoffName);
            if (state != null && handoff != null)
            {
                variants.Add(new PoseVariantHandoff(state, handoff));
            }
        }

        private sealed class PoseLayerRecord
        {
            public AnimatorControllerLayer Layer;
            public List<PoseVariantHandoff> Variants;
        }

        private readonly struct PoseVariantHandoff
        {
            public PoseVariantHandoff(AnimatorState state, AnimatorState handoff)
            {
                State = state;
                Handoff = handoff;
            }

            public AnimatorState State { get; }
            public AnimatorState Handoff { get; }
        }

        private static List<PoseGroupDefinition> ExclusiveResetTargets(PoseGraph graph, PoseGroupDefinition group)
        {
            return graph.Groups
                .Where(other => other != group &&
                                other.Exclusive &&
                                other.Poses.Count > 0 &&
                                PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, other))
                .ToList();
        }

        private static bool NeedsManualCommitGuard(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            List<PoseGroupDefinition> exclusiveResetTargets)
        {
            return group != null &&
                   group.Exclusive &&
                   PoseTuneCompilerRules.AllowsManualControl(root, group) &&
                   exclusiveResetTargets != null &&
                   exclusiveResetTargets.Count > 0;
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

        private static void AddManualGroupDeselectedCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                pose.SelectionValue(root),
                group.ParameterName);
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
                transition.AddCondition(AnimatorConditionMode.Less, 0.5f, activeParameter);
            }
        }

        private static AnimatorState CreateExclusiveCommitState(
            AnimatorControllerLayer layer,
            PoseGroupDefinition group,
            PoseDefinition pose,
            AnimatorState destination,
            HashSet<string> duplicateStateBaseNames,
            List<PoseGroupDefinition> resetTargets,
            List<string> poseActiveParameters,
            bool controlsActionPlayable,
            string activeParameter,
            bool enterPoseSpace,
            int x,
            int y,
            string stateNameSuffix = "",
            int trackingVoteId = 0)
        {
            var commit = layer.stateMachine.AddState(
                "CommitExclusive_" + PoseStateNaming.Name(pose, duplicateStateBaseNames) + stateNameSuffix,
                new Vector3(x, y));
            CopyPoseStateSurface(destination, commit);
            ParameterDriverCompiler.ResetExclusiveGroups(commit, resetTargets);
            ParameterDriverCompiler.ResetPoseActiveParameters(commit, poseActiveParameters);
            if (enterPoseSpace)
            {
                PoseSpaceCompiler.AddEnterPoseSpaceBehavior(commit, pose.PoseSpace);
            }
            if (controlsActionPlayable)
            {
                ParameterDriverCompiler.SetGroupActive(commit, activeParameter, 1f);
            }
            if (trackingVoteId > 0)
            {
                ParameterDriverCompiler.SetTrackingVote(commit, group, trackingVoteId);
            }
            var toPose = commit.AddTransition(destination);
            toPose.hasExitTime = true;
            toPose.exitTime = 0f;
            toPose.duration = CommitStateHoldSeconds(pose);
            return commit;
        }

        private static float CommitStateHoldSeconds(PoseDefinition pose)
        {
            var poseSpaceDelay = pose?.PoseSpace != null && pose.PoseSpace.enabled && pose.PoseSpace.fixedDelay
                ? pose.PoseSpace.delayTime
                : 0f;
            return Mathf.Max(CriticalStateHoldSeconds, poseSpaceDelay);
        }

        private static void CopyPoseStateSurface(AnimatorState source, AnimatorState destination)
        {
            destination.motion = source.motion;
            destination.writeDefaultValues = source.writeDefaultValues;
            destination.timeParameterActive = source.timeParameterActive;
            destination.timeParameter = source.timeParameter;
        }

        private static List<PoseDefinition> PosesForTransitionGeneration(IEnumerable<PoseDefinition> poses)
        {
            return poses
                .OrderByDescending(pose => PriorityRank(pose.Priority))
                .ThenByDescending(pose => pose.Initial)
                .ThenBy(pose => pose.MenuOrder)
                .ThenBy(pose => pose.DisplayName)
                .ThenBy(pose => pose.Id)
                .ToList();
        }

        private static int PriorityRank(PoseClipPriority priority)
        {
            switch (priority)
            {
                case PoseClipPriority.High:
                    return 2;
                case PoseClipPriority.Low:
                    return 0;
                default:
                    return 1;
            }
        }
    }
}
