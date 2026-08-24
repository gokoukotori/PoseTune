using System;
using System.Collections.Generic;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor.Compiler.Conditions
{
    internal static class PoseTuneConditionRule
    {
        private static readonly ConditionOperator[] BoolOperators =
        {
            ConditionOperator.If,
            ConditionOperator.IfNot
        };

        private static readonly ConditionOperator[] IntOperators =
        {
            ConditionOperator.Equals,
            ConditionOperator.NotEquals,
            ConditionOperator.Greater,
            ConditionOperator.Less,
            ConditionOperator.GreaterOrEqual,
            ConditionOperator.LessOrEqual
        };

        private static readonly ConditionOperator[] FloatOperators =
        {
            ConditionOperator.Greater,
            ConditionOperator.Less,
            ConditionOperator.GreaterOrEqual,
            ConditionOperator.LessOrEqual
        };

        public static IReadOnlyList<ConditionOperator> AllowedOperators(ParameterValueType valueType)
        {
            return valueType switch
            {
                ParameterValueType.Bool => BoolOperators,
                ParameterValueType.Int => IntOperators,
                ParameterValueType.Float => FloatOperators,
                _ => Array.Empty<ConditionOperator>()
            };
        }

        public static ConditionOperator DefaultOperator(ParameterValueType valueType)
        {
            return valueType switch
            {
                ParameterValueType.Bool => ConditionOperator.If,
                ParameterValueType.Int => ConditionOperator.Equals,
                ParameterValueType.Float => ConditionOperator.Greater,
                _ => ConditionOperator.Greater
            };
        }

        public static bool IsValid(ParameterConditionData condition)
        {
            return condition != null &&
                   IsDefined(condition.valueType) &&
                   IsDefined(condition.op) &&
                   IsAllowed(condition.valueType, condition.op);
        }

        public static bool IsAllowed(ParameterValueType valueType, ConditionOperator op)
        {
            foreach (var allowed in AllowedOperators(valueType))
            {
                if (allowed == op)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsDefined(ParameterValueType valueType)
        {
            return valueType is ParameterValueType.Bool or ParameterValueType.Int or ParameterValueType.Float;
        }

        public static bool IsDefined(ConditionOperator op)
        {
            return op is ConditionOperator.Equals or
                ConditionOperator.NotEquals or
                ConditionOperator.Greater or
                ConditionOperator.Less or
                ConditionOperator.GreaterOrEqual or
                ConditionOperator.LessOrEqual or
                ConditionOperator.If or
                ConditionOperator.IfNot;
        }

        public static void EnsureCompilable(ParameterConditionData condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (string.IsNullOrWhiteSpace(condition.parameter))
            {
                throw new ArgumentException("Condition parameter must not be empty.", nameof(condition));
            }

            if (!IsValid(condition))
            {
                throw new ArgumentException(
                    $"Unsupported condition type/operator combination: {condition.valueType} / {condition.op}.",
                    nameof(condition));
            }
        }
    }
}
