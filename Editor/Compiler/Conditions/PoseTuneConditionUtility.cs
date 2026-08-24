using System;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Conditions
{
    internal static class PoseTuneConditionUtility
    {
        public const float FloatEpsilon = 0.0001f;

        public static ParameterConditionData Copy(ParameterConditionData condition)
        {
            condition ??= new ParameterConditionData();

            return new ParameterConditionData
            {
                parameter = condition.parameter,
                valueType = condition.valueType,
                op = condition.op,
                floatValue = condition.floatValue,
                intValue = condition.intValue
            };
        }

        public static ParameterConditionData Invert(ParameterConditionData condition)
        {
            if (condition == null)
            {
                return null;
            }

            var inverted = Copy(condition);
            switch (condition.op)
            {
                case ConditionOperator.Equals:
                    inverted.op = ConditionOperator.NotEquals;
                    break;
                case ConditionOperator.NotEquals:
                    inverted.op = ConditionOperator.Equals;
                    break;
                case ConditionOperator.Greater:
                case ConditionOperator.GreaterOrEqual:
                    inverted.op = ConditionOperator.LessOrEqual;
                    break;
                case ConditionOperator.Less:
                case ConditionOperator.LessOrEqual:
                    inverted.op = ConditionOperator.GreaterOrEqual;
                    break;
                case ConditionOperator.If:
                    inverted.op = ConditionOperator.IfNot;
                    break;
                case ConditionOperator.IfNot:
                    inverted.op = ConditionOperator.If;
                    break;
            }

            return inverted;
        }

        public static ParameterConditionData InvertForAutoContextExit(ParameterConditionData condition)
        {
            if (condition == null)
            {
                return null;
            }

            var inverted = Copy(condition);
            switch (condition.op)
            {
                case ConditionOperator.Equals:
                    inverted.op = ConditionOperator.NotEquals;
                    break;
                case ConditionOperator.NotEquals:
                    inverted.op = ConditionOperator.Equals;
                    break;
                case ConditionOperator.Greater:
                    inverted.op = ConditionOperator.Less;
                    break;
                case ConditionOperator.GreaterOrEqual:
                    inverted.op = ConditionOperator.Less;
                    AdjustFloatThreshold(inverted, -FloatEpsilon);
                    break;
                case ConditionOperator.Less:
                    inverted.op = ConditionOperator.Greater;
                    break;
                case ConditionOperator.LessOrEqual:
                    inverted.op = ConditionOperator.Greater;
                    AdjustFloatThreshold(inverted, FloatEpsilon);
                    break;
                case ConditionOperator.If:
                    inverted.op = ConditionOperator.IfNot;
                    break;
                case ConditionOperator.IfNot:
                    inverted.op = ConditionOperator.If;
                    break;
            }

            return inverted;
        }

        public static bool Matches(ParameterConditionData condition, PoseTuneParameterSnapshot snapshot)
        {
            if (condition == null ||
                string.IsNullOrWhiteSpace(condition.parameter) ||
                !PoseTuneConditionRule.IsValid(condition))
            {
                return false;
            }

            snapshot ??= new PoseTuneParameterSnapshot();
            switch (condition.op)
            {
                case ConditionOperator.NotEquals:
                    return !Approximately(Value(condition, snapshot), Threshold(condition));
                case ConditionOperator.Greater:
                    return Value(condition, snapshot) > Threshold(condition);
                case ConditionOperator.Less:
                    return Value(condition, snapshot) < Threshold(condition);
                case ConditionOperator.GreaterOrEqual:
                    return Value(condition, snapshot) >= Threshold(condition);
                case ConditionOperator.LessOrEqual:
                    return Value(condition, snapshot) <= Threshold(condition);
                case ConditionOperator.If:
                    return snapshot.Bool(condition.parameter);
                case ConditionOperator.IfNot:
                    return !snapshot.Bool(condition.parameter);
                case ConditionOperator.Equals:
                    return Approximately(Value(condition, snapshot), Threshold(condition));
                default:
                    return false;
            }
        }

        public static void AddAnimatorCondition(AnimatorStateTransition transition, ParameterConditionData condition)
        {
            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            PoseTuneConditionRule.EnsureCompilable(condition);

            switch (condition.op)
            {
                case ConditionOperator.Equals:
                    transition.AddCondition(AnimatorConditionMode.Equals, AnimatorThreshold(condition), condition.parameter);
                    break;
                case ConditionOperator.NotEquals:
                    transition.AddCondition(AnimatorConditionMode.NotEqual, AnimatorThreshold(condition), condition.parameter);
                    break;
                case ConditionOperator.Greater:
                    transition.AddCondition(AnimatorConditionMode.Greater, AnimatorThreshold(condition), condition.parameter);
                    break;
                case ConditionOperator.GreaterOrEqual:
                    transition.AddCondition(AnimatorConditionMode.Greater,
                        AnimatorThreshold(condition) - FloatEpsilon, condition.parameter);
                    break;
                case ConditionOperator.Less:
                    transition.AddCondition(AnimatorConditionMode.Less, AnimatorThreshold(condition), condition.parameter);
                    break;
                case ConditionOperator.LessOrEqual:
                    transition.AddCondition(AnimatorConditionMode.Less,
                        AnimatorThreshold(condition) + FloatEpsilon, condition.parameter);
                    break;
                case ConditionOperator.If:
                    transition.AddCondition(AnimatorConditionMode.If, 0, condition.parameter);
                    break;
                case ConditionOperator.IfNot:
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0, condition.parameter);
                    break;
            }
        }

        private static void AdjustFloatThreshold(ParameterConditionData condition, float delta)
        {
            if (condition.valueType == ParameterValueType.Float)
            {
                condition.floatValue += delta;
            }
        }

        private static float Value(ParameterConditionData condition, PoseTuneParameterSnapshot snapshot)
        {
            switch (condition.valueType)
            {
                case ParameterValueType.Int:
                    return snapshot.Int(condition.parameter);
                default:
                    return snapshot.Float(condition.parameter);
            }
        }

        private static float Threshold(ParameterConditionData condition)
        {
            switch (condition.valueType)
            {
                case ParameterValueType.Int:
                    return condition.intValue;
                default:
                    return condition.floatValue;
            }
        }

        private static float AnimatorThreshold(ParameterConditionData condition)
        {
            return condition.valueType == ParameterValueType.Int ? condition.intValue : condition.floatValue;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) < FloatEpsilon;
        }
    }
}
