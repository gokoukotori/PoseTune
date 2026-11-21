using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ActionWeightLayerCompiler
    {
        public static void Compile(AnimatorBuildResult result, PoseGraph graph, AnimationClip refreshLoop)
        {
            var layer = AnimatorLayerFactory.NewLayer(PoseTuneNames.ActionWeightLayerName);
            var empty = AnimatorLayerFactory.EmptyClip(PoseTuneNames.ActionWeightLayerName + "_Empty");
            result.GeneratedAssets.Add(empty);

            var inactive = AnimatorLayerFactory.AddState(layer.stateMachine, PoseTuneNames.ActionWeightInactiveState,
                new Vector3(240, 80), empty);
            PlayableLayerCompiler.AddActionWeightBehavior(inactive, 0f);
            layer.stateMachine.defaultState = inactive;

            var active = AnimatorLayerFactory.AddState(layer.stateMachine, PoseTuneNames.ActionWeightActiveState,
                new Vector3(520, 80), empty);
            PlayableLayerCompiler.AddActionWeightBehavior(active, 1f);

            var activeParameters = PoseGraphBuildFilter.BuildableGroups(graph)
                .SelectMany(PoseTuneLayerNaming.GroupActiveParameters)
                .ToList();
            foreach (var parameter in activeParameters)
            {
                var enter = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, active);
                ActiveParameterConditions.AddActiveCondition(enter, parameter);
            }

            if (activeParameters.Count > 0)
            {
                var exitByInternalPose = AnimatorTransitionFactory.Immediate(active, inactive);
                foreach (var parameter in activeParameters)
                {
                    ActiveParameterConditions.AddInactiveCondition(exitByInternalPose, parameter);
                }
            }

            TrackingGuardCompiler.AddInvalidExitTransitions(
                active,
                inactive,
                TrackingGuardCompiler.RootEntryProfile(graph.RootComponent));

            // Added last: pose exits and tracking guards above keep priority over the refresh loop.
            PoseRefreshLayerCompiler.AddRefreshLoop(active, refreshLoop);

            result.TargetController.AddLayer(layer);
        }

    }
}
