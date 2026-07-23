using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneConditionValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            if (graph == null || report == null)
            {
                return;
            }

            foreach (var pose in graph.Poses ?? Enumerable.Empty<PoseDefinition>())
            {
                ValidateConditions(pose?.Conditions, pose?.Source, report);
                foreach (var branch in pose?.ConditionBranches ?? new List<List<ParameterConditionData>>())
                {
                    ValidateConditions(branch, pose?.Source, report);
                }
            }

            foreach (var group in graph.Groups ?? Enumerable.Empty<PoseGroupDefinition>())
            {
                ValidateConditions(group?.Conditions, group?.Source, report);
            }
        }

        private static void ValidateConditions(
            IEnumerable<ParameterConditionData> conditions,
            Object context,
            ValidationReport report)
        {
            foreach (var condition in conditions ?? Enumerable.Empty<ParameterConditionData>())
            {
                if (condition == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.ParameterConditionInvalid.Code,
                        "条件データが null です。条件を作り直してください。",
                        context);
                    continue;
                }

                if (!PoseTuneConditionRule.IsDefined(condition.valueType))
                {
                    report.Error(
                        PoseTuneDiagnostics.ParameterConditionInvalid.Code,
                        $"条件 parameter の値の型が不正です: {condition.parameter} ({(int)condition.valueType})。",
                        context);
                    continue;
                }

                if (!PoseTuneConditionRule.IsDefined(condition.op) ||
                    !PoseTuneConditionRule.IsAllowed(condition.valueType, condition.op))
                {
                    report.Error(
                        PoseTuneDiagnostics.ParameterConditionInvalid.Code,
                        $"条件 parameter の型と比較方法の組み合わせが不正です: {condition.parameter} ({condition.valueType} / {condition.op})。",
                        context);
                }
            }
        }
    }
}
