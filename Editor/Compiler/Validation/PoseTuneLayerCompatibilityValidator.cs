using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneLayerCompatibilityValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            ValidateActionWeightConflictRisk(graph, report);
            ValidateBaseLayerLowerBodyPoseRisk(graph, report);
        }

        private static void ValidateActionWeightConflictRisk(PoseGraph graph, ValidationReport report)
        {
            var hasBuildablePoses = PoseGraphBuildFilter.BuildableGroups(graph).Any(group => group.Poses.Count > 0);
            if (!hasBuildablePoses)
            {
                return;
            }

            if (graph.RootComponent.targetLayer == PoseTuneTargetLayer.Action &&
                graph.RootComponent.advancedSettings.actionWeightControlMode == ActionWeightControlMode.Disabled)
            {
                report.Warning(PoseTuneDiagnostics.ActionLayerWeightControlDisabled.Code, "Action Weight 制御が無効なため、外部で Action layer weight を上げないと pose が見えない可能性があります。", graph.RootComponent);
                return;
            }

            if (!PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent))
            {
                return;
            }

            report.Warning(PoseTuneDiagnostics.ActionLayerWeightControlConflictRisk.Code, "Action layer weight を制御するため、他の Action layer gimmick と競合する可能性があります。", graph.RootComponent);
        }

        private static void ValidateBaseLayerLowerBodyPoseRisk(PoseGraph graph, ValidationReport report)
        {
            if (graph.RootComponent.targetLayer != PoseTuneTargetLayer.Base)
            {
                return;
            }

            var hasLowerBodyPose = PoseGraphBuildFilter.BuildableGroups(graph)
                .Any(group => group.Poses.Count > 0 && IsLocomotionSensitiveGroup(group.Kind));
            if (!hasLowerBodyPose)
            {
                return;
            }

            report.Warning(
                PoseTuneDiagnostics.BaseLayerLowerBodyPoseRisk.Code,
                "Base layer は VRChat の locomotion/idle と同じ Transform を更新します。Chair/Floor/Prone/Supine の下半身 pose は静止時に上書き競合しやすいため、Action layer 出力を推奨します。",
                graph.RootComponent);
        }

        private static bool IsLocomotionSensitiveGroup(PoseGroupKind kind)
        {
            return kind == PoseGroupKind.Chair ||
                   kind == PoseGroupKind.Floor ||
                   kind == PoseGroupKind.Prone ||
                   kind == PoseGroupKind.Supine;
        }
    }
}
