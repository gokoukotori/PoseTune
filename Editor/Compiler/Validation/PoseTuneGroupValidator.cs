using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneGroupValidator
    {
        private const int MaxSyncedGroupIntCount = 16;

        public static void ValidateGroups(PoseGraph graph, ValidationReport report)
        {
            foreach (var group in graph.Groups)
            {
                if (group.Poses.Count == 0)
                {
                    var sourcePoseCount = group.Source != null
                        ? PoseGroupOwnership.OwnedClips(group.Source).Count()
                        : 0;
                    if (sourcePoseCount == 0)
                    {
                        report.Warning(PoseTuneDiagnostics.GroupHasNoPose.Code, "PoseGroup に PoseClip がありません。", group.Source);
                    }
                    else
                    {
                        report.Warning(PoseTuneDiagnostics.GroupHasNoEnabledPose.Code, "PoseGroup に有効な PoseClip がありません。", group.Source);
                    }
                }

                ValidateInitialPoseCount(group, report);
                ValidateExclusiveGroup(group, report);
            }
        }

        public static void ValidateParameterConflicts(PoseGraph graph, ValidationReport report)
        {
            var originalNames = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => PoseTuneCompilerRules.RequiresPoseSelectionParameter(graph.RootComponent, group))
                .Where(group => group.Source != null && !string.IsNullOrWhiteSpace(group.Source.parameterName))
                .Select(group => new
                {
                    Group = group,
                    ParameterName = group.Source.parameterName.Trim()
                })
                .ToList();

            foreach (var duplicate in originalNames.GroupBy(item => item.ParameterName).Where(g => g.Count() > 1))
            {
                foreach (var item in duplicate)
                {
                    report.Error(PoseTuneDiagnostics.GroupParameterConflict.Code, "パラメータ名が競合しています: " + duplicate.Key, item.Group.Source);
                }
            }

            foreach (var item in originalNames.Where(item => item.Group.ParameterName != item.ParameterName))
            {
                report.Error(PoseTuneDiagnostics.GroupGeneratedParameterConflict.Code,
                    "明示指定されたパラメータ名が別の生成パラメータと競合しており、リネームされます: " + item.ParameterName,
                    item.Group.Source);
            }
        }

        public static void ValidateSyncedGroupIntCount(PoseGraph graph, ValidationReport report)
        {
            var syncedGroupInts = PoseGraphBuildFilter.BuildableGroups(graph).Count(group =>
                PoseTuneCompilerRules.RequiresPoseSelectionParameter(graph.RootComponent, group) && group.Synced);
            if (syncedGroupInts > MaxSyncedGroupIntCount)
            {
                report.Warning(PoseTuneDiagnostics.GroupSyncedParameterBudgetExceeded.Code,
                    $"PoseTune に同期されるグループ Int パラメータが {syncedGroupInts} 個あります。メニューをまとめるか、一部のグループをローカル専用にすることを検討してください。",
                    graph.RootComponent);
            }
        }

        private static void ValidateExclusiveGroup(PoseGroupDefinition group, ValidationReport report)
        {
            if (group.Exclusive)
            {
                return;
            }

            if (group.Poses.Any(pose => pose.BlendMode == PoseClipBlendMode.Override))
            {
                report.Warning(PoseTuneDiagnostics.GroupNonExclusiveOverridePose.Code, "非排他 group の override pose は他 group と curve 競合する可能性があります。", group.Source);
            }
        }

        private static void ValidateInitialPoseCount(PoseGroupDefinition group, ValidationReport report)
        {
            var initialPoses = group.Poses.Where(p => p.Initial).ToList();
            if (initialPoses.Count <= 1)
            {
                return;
            }

            foreach (var pose in initialPoses)
            {
                report.Warning(PoseTuneDiagnostics.ClipMultipleInitial.Code, "グループ内に複数の初期ポーズがあります。", pose.Source);
            }
        }
    }
}
