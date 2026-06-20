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

            var emitTrackingControl = poses.Any(pose => pose.EmitTrackingControl);
            var controlsTrackingContext = ParameterAllocator.NeedsTrackingContext(graph);
            var exclusiveResetTargets = ExclusiveResetTargets(graph, group);
            var poseActiveParameters = NeedsManualCommitGuard(graph.RootComponent, group, exclusiveResetTargets)
                ? poses.Select(PoseTuneNames.PoseActiveParameter).Distinct().ToList()
                : new List<string>();
            var reset = CreateCleanupState(result, layer, group, "ResetTracking", layerName + "_ResetHold",
                new Vector3(520, 80), emitTrackingControl, controlsActionPlayable, activeParameter,
                poseActiveParameters, controlsTrackingContext);
            var noResetCleanup = poses.Any(pose => !pose.GenerateResetOnExit)
                ? CreateCleanupState(result, layer, group, "ExitCleanupNoReset", layerName + "_NoResetHold",
                    new Vector3(760, 80), false, controlsActionPlayable, activeParameter,
                    poseActiveParameters, controlsTrackingContext)
                : null;

            var autoPose = SelectAutoPose(group);
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
                var hasAutoEntry = AddPoseEntryTransitions(
                    layer,
                    graph,
                    group,
                    pose,
                    autoPose,
                    variants,
                    duplicateStateBaseNames,
                    exclusiveResetTargets,
                    poseActiveParameters,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    x,
                    y);
                var cleanup = CleanupStateForPose(pose, reset, noResetCleanup);
                AddPoseExitTransitions(variants, cleanup, graph, group, pose, hasAutoEntry);

                y += 100;
            }

            AddCleanupReturnTransitions(idle, reset, noResetCleanup);

            result.TargetController.AddLayer(layer);
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
            PoseGroupDefinition group)
        {
            transition.AddCondition(AnimatorConditionMode.Less, 0.5f, group.ParameterName);
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
            int trackingContextId = 0)
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
            if (trackingContextId > 0)
            {
                ParameterDriverCompiler.SetTrackingContext(commit, trackingContextId);
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
