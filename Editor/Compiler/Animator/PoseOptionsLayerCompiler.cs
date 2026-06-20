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
            CreateTrackingOptionsLayer(result, graph);
            CreateLocomotionLockLayer(result, graph);
        }

        private static void CreateTrackingOptionsLayer(AnimatorBuildResult result, PoseGraph graph)
        {
            var layer = AnimatorLayerFactory.NewLayer("PT_TrackingOptions");
            var empty = AnimatorLayerFactory.EmptyClip("PT_TrackingOptions_Empty");
            result.GeneratedAssets.Add(empty);
            var activePoseParameters = PoseTuneLayerNaming.GroupActiveParameters(graph).ToList();
            var states = new Dictionary<int, AnimatorState>();
            for (var mask = 0; mask < 8; mask++)
            {
                var state = layer.stateMachine.AddState(
                    TrackingOptionStateName(mask),
                    new Vector3(240 + mask % 4 * 220, 80 + mask / 4 * 100));
                state.motion = empty;
                TrackingCompiler.AddTrackingBehavior(state, TrackingPolicyForLockMask(mask));
                states[mask] = state;
                if (mask == 0)
                {
                    layer.stateMachine.defaultState = state;
                }
            }

            foreach (var pair in states)
            {
                if (pair.Key == 0)
                {
                    var enterOffWhenLocksDisabled = layer.stateMachine.AddAnyStateTransition(pair.Value);
                    enterOffWhenLocksDisabled.hasExitTime = false;
                    enterOffWhenLocksDisabled.duration = 0f;
                    enterOffWhenLocksDisabled.canTransitionToSelf = false;
                    AddTrackingOptionConditions(enterOffWhenLocksDisabled, graph, pair.Key);
                    continue;
                }

                AddAnyPoseSelectedTransitions(layer, pair.Value, graph, activePoseParameters, pair.Key);
            }

            var enterOffWhenPoseTuneOff = layer.stateMachine.AddAnyStateTransition(states[0]);
            enterOffWhenPoseTuneOff.hasExitTime = false;
            enterOffWhenPoseTuneOff.duration = 0f;
            enterOffWhenPoseTuneOff.canTransitionToSelf = false;
            AddPoseTuneOffCondition(enterOffWhenPoseTuneOff, graph);

            AddNoPoseSelectedTransition(layer, states[0], activePoseParameters);

            result.TargetController.AddLayer(layer);
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

        private static string TrackingOptionStateName(int mask)
        {
            switch (mask)
            {
                case 1:
                    return "LockHead";
                case 2:
                    return "LockHands";
                case 3:
                    return "LockHeadHands";
                case 4:
                    return "LockFeet";
                case 5:
                    return "LockHeadFeet";
                case 6:
                    return "LockHandsFeet";
                case 7:
                    return "LockAll";
                default:
                    return "Off";
            }
        }

        private static TrackingPolicyData TrackingPolicyForLockMask(int mask)
        {
            var policy = TrackingPolicyUtility.NoChange();
            if ((mask & 1) != 0)
            {
                policy.head = TrackingMode.Animation;
            }

            if ((mask & 2) != 0)
            {
                policy.leftHand = TrackingMode.Animation;
                policy.rightHand = TrackingMode.Animation;
                policy.leftFingers = TrackingMode.Animation;
                policy.rightFingers = TrackingMode.Animation;
            }

            if ((mask & 4) != 0)
            {
                policy.leftFoot = TrackingMode.Animation;
                policy.rightFoot = TrackingMode.Animation;
            }

            return policy;
        }

        private static void AddTrackingOptionConditions(AnimatorStateTransition transition, PoseGraph graph, int mask)
        {
            AddBoolCondition(transition, graph.RootComponent.Parameter(PoseTuneNames.LockHead), (mask & 1) != 0);
            AddBoolCondition(transition, graph.RootComponent.Parameter(PoseTuneNames.LockHands), (mask & 2) != 0);
            AddBoolCondition(transition, graph.RootComponent.Parameter(PoseTuneNames.LockFeet), (mask & 4) != 0);
        }

        private static void AddBoolCondition(AnimatorStateTransition transition, string parameter, bool value)
        {
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddAnyPoseSelectedTransitions(
            AnimatorControllerLayer layer,
            AnimatorState state,
            PoseGraph graph,
            IReadOnlyCollection<string> activePoseParameters,
            int mask)
        {
            foreach (var activePoseParameter in activePoseParameters)
            {
                var enter = layer.stateMachine.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = true;
                AddTrackingOptionConditions(enter, graph, mask);
                AddPoseTuneEnabledCondition(enter, graph);
                AddPoseSelectedCondition(enter, activePoseParameter);
            }
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
