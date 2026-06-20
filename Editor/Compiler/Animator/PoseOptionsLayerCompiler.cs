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
            if (ParameterAllocator.NeedsTrackingContext(graph))
            {
                CreateTrackingOptionsLayer(result, graph);
            }

            if (graph.HasPoseOptions)
            {
                CreateLocomotionLockLayer(result, graph);
            }
        }

        private static void CreateTrackingOptionsLayer(AnimatorBuildResult result, PoseGraph graph)
        {
            var layer = AnimatorLayerFactory.NewLayer("PT_TrackingOptions");
            var empty = AnimatorLayerFactory.EmptyClip("PT_TrackingOptions_Empty");
            result.GeneratedAssets.Add(empty);

            var idle = layer.stateMachine.AddState("Off", new Vector3(240, 80));
            idle.motion = empty;
            TrackingCompiler.AddTrackingBehavior(idle, TrackingPolicyUtility.NoChange());
            layer.stateMachine.defaultState = idle;

            var contexts = graph.TrackingContexts.Contexts.ToList();
            for (var contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                var context = contexts[contextIndex];
                var maskCount = graph.HasPoseOptions ? 8 : 1;
                for (var mask = 0; mask < maskCount; mask++)
                {
                    var state = layer.stateMachine.AddState(
                        "C" + context.Id + "_" + TrackingOptionStateName(mask),
                        new Vector3(240 + mask % 4 * 220, 180 + contextIndex * 220 + mask / 4 * 100));
                    state.motion = empty;
                    TrackingCompiler.AddTrackingBehavior(state, TrackingPolicyForLockMask(context.Policy, mask));

                    var enter = layer.stateMachine.AddAnyStateTransition(state);
                    enter.hasExitTime = false;
                    enter.duration = 0f;
                    enter.canTransitionToSelf = true;
                    AddPoseTuneEnabledCondition(enter, graph);
                    AddTrackingContextCondition(enter, context.Id);
                    if (graph.HasPoseOptions)
                    {
                        AddTrackingOptionConditions(enter, graph, mask);
                    }
                }
            }

            var enterOffWhenPoseTuneOff = layer.stateMachine.AddAnyStateTransition(idle);
            enterOffWhenPoseTuneOff.hasExitTime = false;
            enterOffWhenPoseTuneOff.duration = 0f;
            enterOffWhenPoseTuneOff.canTransitionToSelf = false;
            AddPoseTuneOffCondition(enterOffWhenPoseTuneOff, graph);

            var enterOffWhenNoTrackingContext = layer.stateMachine.AddAnyStateTransition(idle);
            enterOffWhenNoTrackingContext.hasExitTime = false;
            enterOffWhenNoTrackingContext.duration = 0f;
            enterOffWhenNoTrackingContext.canTransitionToSelf = false;
            AddNoTrackingContextCondition(enterOffWhenNoTrackingContext);

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

        private static TrackingPolicyData TrackingPolicyForLockMask(TrackingPolicyData basePolicy, int mask)
        {
            var policy = basePolicy != null
                ? TrackingPolicyUtility.Copy(basePolicy)
                : TrackingPolicyUtility.NoChange();
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

        private static void AddTrackingContextCondition(AnimatorStateTransition transition, int contextId)
        {
            transition.AddCondition(AnimatorConditionMode.Equals, contextId, PoseTuneNames.TrackingContext);
        }

        private static void AddNoTrackingContextCondition(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.Equals, 0f, PoseTuneNames.TrackingContext);
        }

        private static void AddBoolCondition(AnimatorStateTransition transition, string parameter, bool value)
        {
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
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
