using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class PoseTuneAnimatorValidator
    {
        public ValidationReport Validate(PoseGraph graph, AnimatorController controller)
        {
            return Validate(graph, controller, new ParameterAllocator().Allocate(graph));
        }

        public ValidationReport Validate(
            PoseGraph graph,
            AnimatorController controller,
            ParameterPlan parameters)
        {
            return Validate(graph, ControllerView.Create(controller), parameters);
        }

        public ValidationReport Validate(
            PoseGraph graph,
            VirtualAnimatorController controller,
            ParameterPlan parameters)
        {
            return Validate(graph, ControllerView.Create(controller), parameters);
        }

        private static ValidationReport Validate(
            PoseGraph graph,
            ControllerView controller,
            ParameterPlan parameters)
        {
            var report = new ValidationReport();
            if (graph?.RootComponent == null || controller == null)
            {
                return report;
            }

            ValidateTrackingParameters(graph, controller, report);
            ValidateTrackingVoteDefinitions(graph, report);
            ValidateTrackingArbiters(graph, controller, report);
            ValidatePoseRefresh(graph, controller, report);
            var poseSelection = parameters?.PoseSelection ?? PoseSelectionPlanner.Build(graph);
            ValidatePoseSelectionParameters(poseSelection, controller, graph, report);

            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph).Where(group => group != null))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    ValidateGroupLayer(
                        graph,
                        controller,
                        group,
                        bucket.LayerName,
                        bucket.Poses?.Where(pose => pose != null).ToList() ?? new List<PoseDefinition>(),
                        poseSelection,
                        report);
                }

                ValidateAutoPreemption(graph, controller, group, poseSelection, report);
            }

            return report;
        }

        private static IEnumerable<ParameterWriteView> ParametersSetBy(StateView state)
        {
            return state?.parameterWrites ?? Array.Empty<ParameterWriteView>();
        }

        private static bool HasCondition(
            TransitionView transition,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            return (transition?.conditions ?? Array.Empty<AnimatorCondition>()).Any(condition =>
                condition.parameter == parameter &&
                condition.mode == mode &&
                Mathf.Approximately(condition.threshold, threshold));
        }

        private static StateView FindState(LayerView layer, string stateName)
        {
            return layer?.stateMachine?.FindState(stateName);
        }

        private static IEnumerable<LayerView> Layers(ControllerView controller)
        {
            return controller?.layers ?? Array.Empty<LayerView>();
        }

        /// <summary>The first layer named <paramref name="layerName"/>, or null.</summary>
        private static LayerView FindLayer(ControllerView controller, string layerName)
        {
            return Layers(controller).FirstOrDefault(candidate => candidate != null && candidate.name == layerName);
        }

        private sealed class ExpectedPoseVariant
        {
            public string StateName;
            public string HandoffName;
            public AnimatorConditionMode? VrModeInvalidExit;
            public float VrModeInvalidExitThreshold;
            public TrackingPolicyData Policy;
        }

        private sealed class ExpectedArbiterVote
        {
            public string Parameter;
            public int VoteId;
            public TrackingMode Mode;
        }

        private sealed class ExpectedAutoPoseRecord
        {
            public PoseDefinition Pose;
            public LayerView Layer;
            public List<ExpectedPoseVariant> Variants;
        }
    }
}
