using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ActionWeightLayerCompiler
    {
        private const float InternalActiveParameterThreshold = 0.5f;

        public static void Compile(AnimatorBuildResult result, PoseGraph graph)
        {
            var layer = AnimatorLayerFactory.NewLayer("PT_ActionWeight");
            var empty = AnimatorLayerFactory.EmptyClip("PT_ActionWeight_Empty");
            result.GeneratedAssets.Add(empty);

            var inactive = layer.stateMachine.AddState("ActionWeightInactive", new Vector3(240, 80));
            inactive.motion = empty;
            PlayableLayerCompiler.AddActionWeightBehavior(inactive, 0f);
            layer.stateMachine.defaultState = inactive;

            var active = layer.stateMachine.AddState("ActionWeightActive", new Vector3(520, 80));
            active.motion = empty;
            PlayableLayerCompiler.AddActionWeightBehavior(active, 1f);

            var activeParameters = PoseGraphBuildFilter.BuildableGroups(graph)
                .SelectMany(PoseTuneLayerNaming.GroupActiveParameters)
                .ToList();
            foreach (var parameter in activeParameters)
            {
                var enter = layer.stateMachine.AddAnyStateTransition(active);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Greater, InternalActiveParameterThreshold, parameter);
            }

            if (activeParameters.Count > 0)
            {
                var exitByInternalPose = active.AddTransition(inactive);
                exitByInternalPose.hasExitTime = false;
                exitByInternalPose.duration = 0f;
                foreach (var parameter in activeParameters)
                {
                    exitByInternalPose.AddCondition(AnimatorConditionMode.Less, InternalActiveParameterThreshold, parameter);
                }
            }

            TrackingGuardCompiler.AddInvalidExitTransitions(
                active,
                inactive,
                TrackingGuardCompiler.RootEntryProfile(graph.RootComponent));

            result.TargetController.AddLayer(layer);
        }

    }
}
