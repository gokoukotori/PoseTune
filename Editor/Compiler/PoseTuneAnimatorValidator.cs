using Gokoukotori.PoseTune;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneAnimatorValidator
    {
        public ValidationReport Validate(PoseGraph graph, AnimatorController controller)
        {
            var report = new ValidationReport();
            if (graph?.RootComponent == null || controller == null)
            {
                return report;
            }

            if (!HasTrackingResetLayer(controller))
            {
                report.Error(PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code, "Tracking Reset State が見つかりません。", graph.RootComponent);
            }

            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    ValidateGroupLayer(graph, controller, bucket.LayerName, bucket.Poses.ToList(), report);
                }
            }

            return report;
        }

        private static void ValidateGroupLayer(
            PoseGraph graph,
            AnimatorController controller,
            string layerName,
            List<PoseDefinition> poses,
            ValidationReport report)
        {
            var layer = controller.layers.FirstOrDefault(l => l.name == layerName);
            if (layer == null || layer.stateMachine == null)
            {
                return;
            }

            var resetState = FindState(layer, "ResetTracking");
            var duplicateStateBaseNames = PoseStateNaming.DuplicateBaseNames(poses);
            foreach (var pose in poses)
            {
                var poseState = FindState(layer, PoseStateNaming.Name(pose, duplicateStateBaseNames));
                if (poseState == null)
                {
                    continue;
                }

                if (resetState == null || !poseState.transitions.Any(t => t.destinationState == resetState))
                {
                    report.Warning(PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code, "Pose state に ResetTracking への終了遷移がありません。", pose.Source);
                }

                if (graph.RootComponent.disableWhenFullBodyTracking &&
                    !graph.RootComponent.advancedSettings.allowFullBodyTracking &&
                    layer.stateMachine.anyStateTransitions
                        .Where(t => t.destinationState == poseState)
                        .Any(t => !HasFbtGuard(t)))
                {
                    report.Warning(PoseTuneDiagnostics.AnimatorFbtPoseEntryRisk.Code, "FBT ユーザーがこのポーズに入る可能性があります。", pose.Source);
                }
            }
        }

        private static bool HasTrackingResetLayer(AnimatorController controller)
        {
            var resetLayer = controller.layers.FirstOrDefault(l => l.name == "PT_ResetTracking");
            return resetLayer != null &&
                   resetLayer.stateMachine != null &&
                   resetLayer.stateMachine.states.Any(s => s.state.name.Contains("ResetTracking"));
        }

        private static AnimatorState FindState(AnimatorControllerLayer layer, string stateName)
        {
            return layer.stateMachine.states
                .Select(s => s.state)
                .FirstOrDefault(s => s != null && s.name.Contains(stateName));
        }

        private static bool HasFbtGuard(AnimatorStateTransition transition)
        {
            var hasUpperGuard = transition.conditions.Any(c =>
                c.parameter == "TrackingType" &&
                c.mode == AnimatorConditionMode.Less &&
                c.threshold < 4.0001f);
            var hasLowerGuard = transition.conditions.Any(c =>
                c.parameter == "TrackingType" &&
                c.mode == AnimatorConditionMode.Greater &&
                c.threshold > 1.9999f);
            return hasUpperGuard && hasLowerGuard;
        }
    }
}
