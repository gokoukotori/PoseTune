using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void CompileTrackingResetLayer(AnimatorBuildResult result)
        {
            var resetLayer = AnimatorLayerFactory.NewLayer("PT_ResetTracking");
            var resetState = resetLayer.stateMachine.AddState("PT_ResetTracking", new Vector3(240, 80));
            var resetLayerHold = AnimatorLayerFactory.ResetHoldClip("PT_ResetTracking_Hold", CriticalStateHoldSeconds);
            resetState.motion = resetLayerHold;
            result.GeneratedAssets.Add(resetLayerHold);
            resetLayer.stateMachine.defaultState = resetState;
            TrackingCompiler.AddTrackingBehavior(resetState, TrackingPolicyData.ResetToTracking());
            PoseSpaceCompiler.AddExitPoseSpaceBehavior(resetState);

            result.TargetController.AddLayer(resetLayer);
        }
    }
}
