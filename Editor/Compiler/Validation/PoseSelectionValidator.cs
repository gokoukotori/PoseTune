using System;
using System.Linq;
using Gokoukotori.PoseTune;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseSelectionValidator
    {
        public static void Validate(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            if (graph.RootComponent.poseSelectionSyncMode != PoseSelectionSyncMode.SharedExclusivePoseId)
            {
                return;
            }

            var plan = context.Parameters.PoseSelection;
            var sharedGroups = plan.GroupBindings.Count(binding => binding.Shared);
            var sharedChannels = plan.Channels.Count(channel => channel.Shared);
            var dedicatedGroups = plan.GroupBindings.Count(binding => !binding.Shared);
            report.Information(
                PoseTuneDiagnostics.SharedPoseSelectionSummary.Code,
                $"共有 Pose ID: 対象 {sharedGroups} groups、共有バンク {sharedChannels}、専用フォールバック {dedicatedGroups} groups、" +
                $"選択用の物理 Int {plan.Channels.Count}、共有対象の同期コスト {sharedGroups * 8}→{sharedChannels * 8} bits。",
                graph.RootComponent);

            ReportFallbacks(plan, graph, report);
            foreach (var channel in plan.Channels.Where(channel => channel.Shared && channel.Poses.Count > 255))
            {
                report.Error(
                    PoseTuneDiagnostics.SharedPoseSelectionCapacityExceeded.Code,
                    $"共有 Pose ID {channel.ParameterName} に {channel.Poses.Count} poses があり、Intで表現できる255 posesを超えています。",
                    graph.RootComponent);
            }

            var initialPoses = plan.PoseBindings
                .Where(binding => binding.Group.Shared && binding.Pose.Initial)
                .ToList();
            if (initialPoses.Count > 1)
            {
                report.Error(
                    PoseTuneDiagnostics.SharedPoseSelectionInitialConflict.Code,
                    "共有 Pose ID の初期ポーズが複数あります: " +
                    string.Join(", ", initialPoses.Select(binding => binding.Pose.DisplayName)),
                    graph.RootComponent);
            }

            ValidateExistingExpressionParameters(graph, plan, report);
        }

        private static void ReportFallbacks(
            PoseSelectionPlan plan,
            PoseGraph graph,
            ValidationReport report)
        {
            ReportFallback(plan, graph, report, PoseSelectionFallbackReason.NonExclusive, "non-exclusive");
            ReportFallback(plan, graph, report, PoseSelectionFallbackReason.NotSynced, "synced=false");
            ReportFallback(plan, graph, report, PoseSelectionFallbackReason.ExplicitParameterName, "明示parameterName");
            ReportFallback(plan, graph, report, PoseSelectionFallbackReason.SelectedPoseAuto, "Auto SelectedPosePerGroup");
        }

        private static void ReportFallback(
            PoseSelectionPlan plan,
            PoseGraph graph,
            ValidationReport report,
            PoseSelectionFallbackReason reason,
            string label)
        {
            var groups = plan.GroupBindings
                .Where(binding => (binding.FallbackReason & reason) != 0)
                .Select(binding => binding.Group.DisplayName)
                .ToList();
            if (groups.Count == 0)
            {
                return;
            }

            report.Information(
                PoseTuneDiagnostics.SharedPoseSelectionSummary.Code,
                $"{label} のため専用 Int を維持します ({groups.Count}): {string.Join(", ", groups)}",
                graph.RootComponent);
        }

        private static void ValidateExistingExpressionParameters(
            PoseGraph graph,
            PoseSelectionPlan plan,
            ValidationReport report)
        {
            var existing = graph.AvatarDescriptor != null
                ? graph.AvatarDescriptor.expressionParameters
                : null;
            if (existing?.parameters == null)
            {
                return;
            }

            foreach (var channel in plan.Channels.Where(channel => channel.Shared))
            {
                foreach (var parameter in existing.parameters.Where(parameter =>
                             parameter != null &&
                             string.Equals(parameter.name, channel.ParameterName, StringComparison.Ordinal)))
                {
                    if (parameter.valueType == VRCExpressionParameters.ValueType.Int &&
                        parameter.saved == channel.Saved &&
                        parameter.networkSynced == channel.Synced)
                    {
                        continue;
                    }

                    report.Error(
                        PoseTuneDiagnostics.SharedPoseSelectionMetadataConflict.Code,
                        $"既存 Expression Parameter {channel.ParameterName} が共有 Pose ID の要件と一致しません。期待: Int, saved={channel.Saved}, synced={channel.Synced}。",
                        graph.RootComponent);
                }
            }
        }
    }
}
