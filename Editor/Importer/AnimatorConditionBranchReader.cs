using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorConditionBranchReader
    {
        public static List<List<ParameterConditionData>> ReadIncomingConditionBranches(
            AnimatorStateMachine stateMachine,
            AnimatorState state)
        {
            var branches = new List<List<ParameterConditionData>>();
            if (stateMachine == null || state == null)
            {
                return branches;
            }

            foreach (var transition in stateMachine.anyStateTransitions.Where(t => t.destinationState == state))
            {
                AddBranch(branches, transition.conditions);
            }

            foreach (var transition in stateMachine.entryTransitions.Where(t => t.destinationState == state))
            {
                AddBranch(branches, transition.conditions);
            }

            foreach (var other in stateMachine.states.Select(s => s.state).Where(s => s != null))
            {
                foreach (var transition in other.transitions.Where(t => t.destinationState == state))
                {
                    AddBranch(branches, transition.conditions);
                }
            }

            return branches;
        }

        public static List<ParameterConditionData> FlattenConditionBranches(
            List<List<ParameterConditionData>> branches)
        {
            if (branches.Count == 0 || branches.Any(branch => branch.Count == 0))
            {
                return new List<ParameterConditionData>();
            }

            return branches.Count == 1
                ? branches[0].Select(PoseTuneConditionUtility.Copy).ToList()
                : branches.SelectMany(branch => branch.Select(PoseTuneConditionUtility.Copy)).ToList();
        }

        private static void AddBranch(
            List<List<ParameterConditionData>> output,
            IEnumerable<AnimatorCondition> conditions)
        {
            var branch = new List<ParameterConditionData>();
            foreach (var condition in conditions)
            {
                if (string.IsNullOrWhiteSpace(condition.parameter))
                {
                    continue;
                }

                branch.Add(ToParameterCondition(condition));
            }

            output.Add(branch);
        }

        private static ParameterConditionData ToParameterCondition(AnimatorCondition condition)
        {
            var data = new ParameterConditionData
            {
                parameter = condition.parameter
            };
            switch (condition.mode)
            {
                case AnimatorConditionMode.If:
                    data.valueType = ParameterValueType.Bool;
                    data.op = ConditionOperator.If;
                    data.boolValue = true;
                    break;
                case AnimatorConditionMode.IfNot:
                    data.valueType = ParameterValueType.Bool;
                    data.op = ConditionOperator.IfNot;
                    data.boolValue = false;
                    break;
                case AnimatorConditionMode.Greater:
                    data.valueType = ParameterValueType.Float;
                    data.op = ConditionOperator.Greater;
                    data.floatValue = condition.threshold;
                    break;
                case AnimatorConditionMode.Less:
                    data.valueType = ParameterValueType.Float;
                    data.op = ConditionOperator.Less;
                    data.floatValue = condition.threshold;
                    break;
                case AnimatorConditionMode.NotEqual:
                    ApplyNumericCondition(data, ConditionOperator.NotEquals, condition.threshold);
                    break;
                default:
                    ApplyNumericCondition(data, ConditionOperator.Equals, condition.threshold);
                    break;
            }

            return data;
        }

        private static void ApplyNumericCondition(
            ParameterConditionData data,
            ConditionOperator op,
            float threshold)
        {
            data.op = op;
            var rounded = Mathf.RoundToInt(threshold);
            if (Mathf.Abs(threshold - rounded) < 0.0001f)
            {
                data.valueType = ParameterValueType.Int;
                data.intValue = rounded;
                data.floatValue = rounded;
                return;
            }

            data.valueType = ParameterValueType.Float;
            data.floatValue = threshold;
        }
    }
}
