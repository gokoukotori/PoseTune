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
            PoseGroupDefinition group,
            string stateName,
            string clipName,
            Vector3 position,
            bool addTrackingReset,
            bool controlsActionPlayable,
            string activeParameter,
            List<string> poseActiveParameters,
            bool controlsTrackingContext)
        {
            var state = layer.stateMachine.AddState(stateName, position);
            var hold = AnimatorLayerFactory.ResetHoldClip(clipName, CriticalStateHoldSeconds);
            state.motion = hold;
            result.GeneratedAssets.Add(hold);
            if (addTrackingReset)
            {
                TrackingCompiler.AddTrackingBehavior(state, TrackingPolicyData.ResetToTracking());
            }

            PoseSpaceCompiler.AddExitPoseSpaceBehavior(state);
            if (controlsActionPlayable)
            {
                ParameterDriverCompiler.SetGroupActive(state, activeParameter, 0f);
            }
            if (controlsTrackingContext)
            {
                ParameterDriverCompiler.SetTrackingContext(state, 0);
            }
            ParameterDriverCompiler.ResetPoseActiveParameters(state, poseActiveParameters);

            return state;
        }

        private static AnimatorState CleanupStateForPose(
            PoseDefinition pose,
            AnimatorState reset,
            AnimatorState noResetCleanup)
        {
            return pose.GenerateResetOnExit || noResetCleanup == null ? reset : noResetCleanup;
        }

        private static void AddCleanupReturnTransitions(
            AnimatorState idle,
            AnimatorState reset,
            AnimatorState noResetCleanup)
        {
            var resetToIdle = reset.AddTransition(idle);
            resetToIdle.hasExitTime = true;
            resetToIdle.exitTime = 1f;
            resetToIdle.duration = 0f;

            if (noResetCleanup == null)
            {
                return;
            }

            var noResetToIdle = noResetCleanup.AddTransition(idle);
            noResetToIdle.hasExitTime = true;
            noResetToIdle.exitTime = 1f;
            noResetToIdle.duration = 0f;
        }
    }
}
