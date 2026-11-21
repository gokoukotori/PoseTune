using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseLayerCompiler
    {
        internal static void CreateActionPoseLayer(
            AnimatorBuildResult result,
            PoseGraph graph,
            PoseGroupDefinition group,
            List<PoseDefinition> poses,
            string layerName,
            PoseClipBlendMode blendMode,
            string activeParameter,
            AnimationClip handoffHold)
        {
            var layer = AnimatorLayerFactory.NewLayer(layerName);
            layer.blendingMode = blendMode == PoseClipBlendMode.Additive
                ? AnimatorLayerBlendingMode.Additive
                : AnimatorLayerBlendingMode.Override;
            var empty = AnimatorLayerFactory.EmptyClip("PT_Empty");
            var idle = AnimatorLayerFactory.AddState(layer.stateMachine, "PassThrough", new Vector3(240, 80), empty);
            result.GeneratedAssets.Add(empty);
            layer.stateMachine.defaultState = idle;

            var poseSelection = result.Parameters?.PoseSelection ?? PoseSelectionPlanner.Build(graph);
            var exclusiveResetTargets = poseSelection.ExclusiveResetParameterNames(
                graph.RootComponent,
                PoseGraphBuildFilter.BuildableGroups(graph),
                group).ToList();
            var poseActiveParameters = NeedsManualCommitGuard(graph.RootComponent, group, exclusiveResetTargets)
                ? poses.Select(PoseTuneNames.PoseActiveParameter).Distinct().ToList()
                : new List<string>();
            var x = 240;
            var y = 180;
            var orderedPoses = PosesForTransitionGeneration(poses);
            var context = new PoseLayerBuildContext
            {
                Result = result,
                Layer = layer,
                Graph = graph,
                Group = group,
                DuplicateStateBaseNames = PoseStateNaming.DuplicateBaseNames(orderedPoses),
                ExclusiveResetTargets = exclusiveResetTargets,
                PoseActiveParameters = poseActiveParameters,
                ActiveParameter = activeParameter,
                HandoffHold = handoffHold,
                PoseSelection = poseSelection
            };
            foreach (var pose in orderedPoses)
            {
                var poseActiveParameter = poseActiveParameters.Contains(PoseTuneNames.PoseActiveParameter(pose))
                    ? PoseTuneNames.PoseActiveParameter(pose)
                    : "";
                var variants = PoseStateFactory.CreateVariants(context, pose, poseActiveParameter, x, y);
                foreach (var variant in variants.All)
                {
                    variant.Handoff = PoseHandoffStateFactory.Create(context, pose, variant, x, y);
                }

                var hasAutoEntry = PoseEntryTransitionBuilder.AddPoseEntryTransitions(
                    context,
                    pose,
                    variants,
                    poseActiveParameter,
                    x,
                    y);
                PoseExitTransitionBuilder.AddPoseExitTransitions(context, pose, variants, hasAutoEntry);
                foreach (var variant in variants.All)
                {
                    AnimatorTransitionFactory.AfterExitTime(variant.Handoff, idle);
                }

                y += 100;
            }

            result.TargetController.AddLayer(layer);
        }

        private static bool NeedsManualCommitGuard(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            List<string> exclusiveResetTargets)
        {
            return group != null &&
                   group.Exclusive &&
                   PoseTuneCompilerRules.AllowsManualControl(root, group) &&
                   exclusiveResetTargets != null &&
                   exclusiveResetTargets.Count > 0;
        }

        internal static List<PoseDefinition> PosesForTransitionGeneration(IEnumerable<PoseDefinition> poses)
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
