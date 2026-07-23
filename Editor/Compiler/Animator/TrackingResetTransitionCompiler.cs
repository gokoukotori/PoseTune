using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static AnimatorState CreateCleanupState(
            AnimatorBuildResult result,
            AnimatorControllerLayer layer,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            string stateName,
            string clipName,
            Vector3 position,
            bool controlsActionPlayable,
            string activeParameter,
            List<string> poseActiveParameters,
            TrackingPolicyData outgoingPolicy)
        {
            var state = layer.stateMachine.AddState(stateName, position);
            var hold = AnimatorLayerFactory.ResetHoldClip(clipName, CriticalStateHoldSeconds);
            state.motion = hold;
            result.GeneratedAssets.Add(hold);
            if (pose.EmitTrackingControl && ParameterAllocator.RequiresTrackingVote(graph, group))
            {
                ParameterDriverCompiler.SetTrackingVote(state, group, 0);
            }

            if (pose.GenerateResetOnExit)
            {
                ParameterDriverCompiler.RequestTrackingReset(
                    state,
                    TrackingArbiterCompiler.ControlledParts(outgoingPolicy));
            }

            PoseSpaceCompiler.AddExitPoseSpaceBehavior(state);
            if (controlsActionPlayable)
            {
                ParameterDriverCompiler.SetGroupActive(state, activeParameter, 0f);
            }
            ParameterDriverCompiler.ResetPoseActiveParameters(state, poseActiveParameters);

            return state;
        }

        private static void AddCleanupReturnTransition(
            AnimatorState idle,
            AnimatorState cleanup)
        {
            var toIdle = cleanup.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;
        }
    }
}
