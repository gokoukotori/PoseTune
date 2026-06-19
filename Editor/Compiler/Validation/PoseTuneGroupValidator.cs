using System.Collections.Generic;
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
                        ? group.Source.GetComponentsInChildren<PoseClip>(true).Length
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
                ValidateAutoPriorityAmbiguity(graph, group, report);
            }
        }

        public static void ValidateParameterConflicts(PoseGraph graph, ValidationReport report)
        {
            var originalNames = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
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
                PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group) && group.Synced);
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

        private static void ValidateAutoPriorityAmbiguity(PoseGraph graph, PoseGroupDefinition group, ValidationReport report)
        {
            if (!graph.RootComponent.enableAutoContextSwitch ||
                group.ActivationMode == PoseGroupActivationMode.Manual ||
                group.AutoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup)
            {
                return;
            }

            foreach (var duplicate in group.Poses
                         .Where(pose => pose.ConditionBranches.Count == 0 ||
                                        pose.ConditionBranches.Any(branch => branch.Count > 0) ||
                                        group.Kind != PoseGroupKind.Custom)
                         .GroupBy(pose => pose.Priority + ":" + ConditionKey(pose))
                         .Where(candidate => candidate.Count() > 1))
            {
                foreach (var pose in duplicate)
                {
                    report.Warning(PoseTuneDiagnostics.AutoPosePriorityAmbiguous.Code, "同条件・同 priority の auto pose が複数あります。auto pose の優先順位が曖昧です。", pose.Source);
                }
            }
        }

        private static string ConditionKey(PoseDefinition pose)
        {
            var branches = pose.ConditionBranches.Count > 0
                ? pose.ConditionBranches
                : new List<List<ParameterConditionData>> { pose.Conditions };
            return string.Join("|", branches.Select(branch => string.Join("&", (branch ?? new List<ParameterConditionData>())
                .OrderBy(condition => condition.parameter)
                .ThenBy(condition => condition.op)
                .Select(condition => $"{condition.parameter}:{condition.valueType}:{condition.op}:{condition.floatValue}:{condition.intValue}:{condition.boolValue}"))));
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
