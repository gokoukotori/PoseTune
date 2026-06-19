using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public static class ImportCandidateDisplay
    {
        public static string Summary(ImportCandidate candidate)
        {
            if (candidate == null)
            {
                return "";
            }

            var builder = new StringBuilder();
            builder.Append("Source: ");
            builder.Append(string.IsNullOrWhiteSpace(candidate.SourceLayerName)
                ? "Unknown"
                : candidate.SourceLayerName);
            builder.Append(" (index ");
            builder.Append(candidate.SourceLayerIndex);
            builder.Append(")");
            builder.Append(" / Target: ");
            builder.Append(candidate.Target);
            builder.Append(" / Confidence: ");
            builder.Append(Mathf.RoundToInt(Mathf.Clamp01(candidate.Confidence) * 100f));
            builder.Append("%");

            if (!string.IsNullOrWhiteSpace(candidate.DisabledReason))
            {
                builder.Append(" / Disabled: ");
                builder.Append(candidate.DisabledReason);
            }

            if (candidate.FromBlendTree && candidate.BlendTreePath.Count > 0)
            {
                builder.Append(" / BlendTree: ");
                builder.Append(string.Join(" > ", candidate.BlendTreePath.Select(TreeSummary)));
            }

            if (candidate.ConfidenceReasons.Count > 0)
            {
                builder.Append(" / Reasons: ");
                builder.Append(string.Join(", ", candidate.ConfidenceReasons));
            }

            var conditions = ConditionBranches(candidate).ToList();
            if (conditions.Count > 0)
            {
                builder.Append(" / Conditions: ");
                builder.Append(string.Join("; ", conditions.Select((branch, index) =>
                    "branch " + (index + 1) + ": " + BranchSummary(branch))));
            }

            return builder.ToString();
        }

        private static IEnumerable<List<ParameterConditionData>> ConditionBranches(ImportCandidate candidate)
        {
            if (candidate.ConditionBranches != null && candidate.ConditionBranches.Count > 0)
            {
                return candidate.ConditionBranches.Where(branch => branch != null);
            }

            return candidate.Conditions != null && candidate.Conditions.Count > 0
                ? new[] { candidate.Conditions }
                : Enumerable.Empty<List<ParameterConditionData>>();
        }

        private static string BranchSummary(List<ParameterConditionData> branch)
        {
            return branch.Count == 0
                ? "<unconditional>"
                : string.Join(", ", branch.Select(ConditionSummary));
        }

        private static string TreeSummary(BlendTreeChildInfo info)
        {
            return info.BlendTreeName + "(" + info.BlendParameter + "=" + info.Threshold.ToString("0.###") + ")";
        }

        private static string ConditionSummary(ParameterConditionData condition)
        {
            if (condition == null)
            {
                return "<null>";
            }

            return string.IsNullOrWhiteSpace(condition.parameter)
                ? "<unnamed>"
                : condition.parameter + " " + OperatorText(condition.op) + " " + ValueText(condition);
        }

        private static string OperatorText(ConditionOperator op)
        {
            switch (op)
            {
                case ConditionOperator.NotEquals:
                    return "!=";
                case ConditionOperator.Greater:
                    return ">";
                case ConditionOperator.Less:
                    return "<";
                case ConditionOperator.GreaterOrEqual:
                    return ">=";
                case ConditionOperator.LessOrEqual:
                    return "<=";
                case ConditionOperator.If:
                    return "is";
                case ConditionOperator.IfNot:
                    return "is not";
                default:
                    return "==";
            }
        }

        private static string ValueText(ParameterConditionData condition)
        {
            switch (condition.valueType)
            {
                case ParameterValueType.Bool:
                    return condition.op == ConditionOperator.IfNot ? "true" : condition.boolValue.ToString();
                case ParameterValueType.Int:
                    return condition.intValue.ToString();
                default:
                    return condition.floatValue.ToString("0.###");
            }
        }
    }
}
