using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class PoseTuneAnimatorValidator
    {
        private static void ValidateTrackingParameters(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            var parameters = controller.parameters ?? Array.Empty<AnimatorControllerParameter>();
            foreach (var (name, type) in RequiredTrackingParameters(graph))
            {
                ValidateParameter(parameters, name, type, graph.RootComponent, report);
            }
        }

        /// <summary>
        /// Lazily yields, in diagnostic order, the tracking-vote, tracking-reset, pose-refresh and group-active
        /// parameters the controller must declare.
        /// </summary>
        private static IEnumerable<(string Name, AnimatorControllerParameterType Type)> RequiredTrackingParameters(
            PoseGraph graph)
        {
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null && ParameterAllocator.RequiresTrackingVote(graph, group)))
            {
                yield return (PoseTuneNames.TrackingVoteParameter(group), AnimatorControllerParameterType.Int);
            }

            foreach (var part in TrackingArbiterCompiler.RequiredParts(graph))
            {
                yield return (PoseTuneNames.TrackingResetParameter(part), AnimatorControllerParameterType.Bool);
            }

            if (PoseRefreshLayerCompiler.IsNeeded(graph))
            {
                yield return (PoseTuneNames.PoseRefreshParameter, AnimatorControllerParameterType.Bool);
            }

            foreach (var activeParameter in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null)
                         .SelectMany(PoseTuneLayerNaming.GroupActiveParameters)
                         .Distinct())
            {
                yield return (activeParameter, AnimatorControllerParameterType.Float);
            }
        }

        private static void ValidatePoseSelectionParameters(
            PoseSelectionPlan poseSelection,
            ControllerView controller,
            PoseGraph graph,
            ValidationReport report)
        {
            var parameters = controller.parameters ?? Array.Empty<AnimatorControllerParameter>();
            foreach (var channel in poseSelection.Channels)
            {
                if (HasParameter(parameters, channel.ParameterName, AnimatorControllerParameterType.Int))
                {
                    continue;
                }

                report.Error(
                    PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                    $"Pose selection parameter がないか型が不正です: {channel.ParameterName} (Int)",
                    graph.RootComponent);
            }
        }

        private static void ValidateParameter(
            IEnumerable<AnimatorControllerParameter> parameters,
            string name,
            AnimatorControllerParameterType expectedType,
            UnityEngine.Object context,
            ValidationReport report)
        {
            if (!HasParameter(parameters, name, expectedType))
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Tracking parameter がないか型が不正です: {name} ({expectedType})",
                    context);
            }
        }

        /// <summary>
        /// True when the first non-null parameter named <paramref name="name"/> has <paramref name="type"/>; a later
        /// same-named parameter of the right type does not count.
        /// </summary>
        private static bool HasParameter(
            IEnumerable<AnimatorControllerParameter> parameters,
            string name,
            AnimatorControllerParameterType type)
        {
            var parameter = parameters.FirstOrDefault(candidate => candidate != null && candidate.name == name);
            return parameter != null && parameter.type == type;
        }
    }
}
