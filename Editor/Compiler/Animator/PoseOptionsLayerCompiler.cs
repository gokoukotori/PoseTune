using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseOptionsLayerCompiler
    {
        public static void Compile(AnimatorBuildResult result, PoseGraph graph)
        {
            if (graph.HasPoseOptions)
            {
                CreateLocomotionLockLayer(result, graph);
            }
        }

        private static void CreateLocomotionLockLayer(AnimatorBuildResult result, PoseGraph graph)
        {
            var layer = AnimatorLayerFactory.NewLayer("PT_LocomotionLock");
            var empty = AnimatorLayerFactory.EmptyClip("PT_LocomotionLock_Empty");
            result.GeneratedAssets.Add(empty);
            var activePoseParameters = PoseTuneLayerNaming.GroupActiveParameters(graph).ToList();

            var enable = AnimatorLayerFactory.AddState(layer.stateMachine, "LocomotionEnable",
                new Vector3(240, 80), empty);
            LocomotionCompiler.AddLocomotionBehavior(enable, false);
            layer.stateMachine.defaultState = enable;

            var disable = AnimatorLayerFactory.AddState(layer.stateMachine, "LocomotionDisable",
                new Vector3(520, 80), empty);
            LocomotionCompiler.AddLocomotionBehavior(disable, true);

            var parameter = graph.RootComponent.Parameter(PoseTuneNames.LocomotionLock);
            var enterEnable = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, enable);
            enterEnable.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter);

            foreach (var activePoseParameter in activePoseParameters)
            {
                var enterDisable = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, disable);
                enterDisable.AddCondition(AnimatorConditionMode.If, 0f, parameter);
                AddPoseTuneEnabledCondition(enterDisable, graph);
                ActiveParameterConditions.AddActiveCondition(enterDisable, activePoseParameter);
            }

            var enterEnableWhenPoseTuneOff = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, enable);
            AddPoseTuneOffCondition(enterEnableWhenPoseTuneOff, graph);

            AddNoPoseSelectedTransition(layer, enable, activePoseParameters);

            result.TargetController.AddLayer(layer);
        }

        private static void AddNoPoseSelectedTransition(
            AnimatorControllerLayer layer,
            AnimatorState state,
            IReadOnlyCollection<string> activePoseParameters)
        {
            if (activePoseParameters.Count == 0)
            {
                return;
            }

            var enter = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, state);
            foreach (var activePoseParameter in activePoseParameters)
            {
                ActiveParameterConditions.AddInactiveCondition(enter, activePoseParameter);
            }
        }

        private static void AddPoseTuneEnabledCondition(AnimatorStateTransition transition, PoseGraph graph)
        {
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                PoseTuneModeValues.Off,
                graph.RootComponent.Parameter(PoseTuneNames.Mode));
        }

        private static void AddPoseTuneOffCondition(AnimatorStateTransition transition, PoseGraph graph)
        {
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                PoseTuneModeValues.Off,
                graph.RootComponent.Parameter(PoseTuneNames.Mode));
        }

    }
}
