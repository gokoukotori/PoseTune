using System.Collections.Generic;
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
                var enter = layer.stateMachine.AddAnyStateTransition(pair.Value);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = pair.Key != 0;
                AddTrackingOptionConditions(enter, graph, pair.Key);
            }

            result.TargetController.AddLayer(layer);
        }

        private static void CreateLocomotionLockLayer(AnimatorBuildResult result, PoseGraph graph)
        {
            var layer = AnimatorLayerFactory.NewLayer("PT_LocomotionLock");
            var empty = AnimatorLayerFactory.EmptyClip("PT_LocomotionLock_Empty");
            result.GeneratedAssets.Add(empty);

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

            var enterDisable = layer.stateMachine.AddAnyStateTransition(disable);
            enterDisable.hasExitTime = false;
            enterDisable.duration = 0f;
            enterDisable.canTransitionToSelf = false;
            enterDisable.AddCondition(AnimatorConditionMode.If, 0f, parameter);

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

    }
}
