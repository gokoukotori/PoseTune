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

            var enable = layer.stateMachine.AddState("LocomotionEnable", new Vector3(240, 80));
            enable.motion = empty;
            LocomotionCompiler.AddLocomotionBehavior(enable, false);
            layer.stateMachine.defaultState = enable;

            var disable = layer.stateMachine.AddState("LocomotionDisable", new Vector3(520, 80));
            disable.motion = empty;
            LocomotionCompiler.AddLocomotionBehavior(disable, true);

            var parameter = graph.RootComponent.Parameter(PoseTuneNames.LocomotionLock);
            var enterEnable = layer.stateMachine.AddAnyStateTransition(enable);
            enterEnable.hasExitTime = false;
            enterEnable.duration = 0f;
            enterEnable.canTransitionToSelf = false;
            enterEnable.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter);

            foreach (var activePoseParameter in activePoseParameters)
            {
                var enterDisable = layer.stateMachine.AddAnyStateTransition(disable);
                enterDisable.hasExitTime = false;
                enterDisable.duration = 0f;
                enterDisable.canTransitionToSelf = false;
                enterDisable.AddCondition(AnimatorConditionMode.If, 0f, parameter);
                AddPoseTuneEnabledCondition(enterDisable, graph);
                AddPoseSelectedCondition(enterDisable, activePoseParameter);
            }

            var enterEnableWhenPoseTuneOff = layer.stateMachine.AddAnyStateTransition(enable);
            enterEnableWhenPoseTuneOff.hasExitTime = false;
            enterEnableWhenPoseTuneOff.duration = 0f;
            enterEnableWhenPoseTuneOff.canTransitionToSelf = false;
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

            var enter = layer.stateMachine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0f;
            enter.canTransitionToSelf = false;
            foreach (var activePoseParameter in activePoseParameters)
            {
                AddPoseDeselectedCondition(enter, activePoseParameter);
            }
        }

        private static void AddPoseSelectedCondition(AnimatorStateTransition transition, string parameter)
        {
            transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, parameter);
        }

        private static void AddPoseDeselectedCondition(AnimatorStateTransition transition, string parameter)
        {
            transition.AddCondition(AnimatorConditionMode.Less, 0.5f, parameter);
        }

        private static void AddPoseTuneEnabledCondition(AnimatorStateTransition transition, PoseGraph graph)
        {
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                0f,
                graph.RootComponent.Parameter(PoseTuneNames.Mode));
        }

        private static void AddPoseTuneOffCondition(AnimatorStateTransition transition, PoseGraph graph)
        {
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                0f,
                graph.RootComponent.Parameter(PoseTuneNames.Mode));
        }

    }
}
