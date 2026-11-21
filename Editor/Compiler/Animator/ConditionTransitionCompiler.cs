using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ConditionTransitionCompiler
    {
        internal static void AddConditionExitTransitions(AnimatorState state, AnimatorState reset, PoseDefinition pose)
        {
            foreach (var conditionSet in InvertConditionBranches(pose))
            {
                var transition = AnimatorTransitionFactory.Immediate(state, reset);
                AddConditions(transition, conditionSet);
            }
        }

        /// <summary>
        /// The pose's OR-ed condition branches; a pose without branches has its flat condition list as its only
        /// branch.
        /// </summary>
        internal static List<List<ParameterConditionData>> EffectiveConditionBranches(PoseDefinition pose)
        {
            return pose.ConditionBranches.Count > 0
                ? pose.ConditionBranches
                : new List<List<ParameterConditionData>> { pose.Conditions };
        }

        private static List<List<ParameterConditionData>> InvertConditionBranches(PoseDefinition pose)
        {
            var branches = EffectiveConditionBranches(pose);
            branches = branches.Where(branch => branch != null).ToList();
            if (branches.Count == 0 || branches.Any(branch => branch.Count == 0))
            {
                return new List<List<ParameterConditionData>>();
            }

            var result = new List<List<ParameterConditionData>> { new() };
            foreach (var branch in branches)
            {
                var options = branch
                    .Where(condition => !string.IsNullOrWhiteSpace(condition.parameter))
                    .Select(PoseTuneConditionUtility.InvertForAutoContextExit)
                    .Where(condition => condition != null)
                    .ToList();
                if (options.Count == 0)
                {
                    return new List<List<ParameterConditionData>>();
                }

                var next = new List<List<ParameterConditionData>>();
                foreach (var existing in result)
                {
                    foreach (var option in options)
                    {
                        var combined = new List<ParameterConditionData>(existing) { option };
                        next.Add(combined);
                    }
                }

                result = next;
            }

            return result;
        }

        internal static void AddConditions(AnimatorStateTransition transition, IEnumerable<ParameterConditionData> conditions)
        {
            foreach (var condition in conditions ?? Enumerable.Empty<ParameterConditionData>())
            {
                PoseTuneConditionUtility.AddAnimatorCondition(transition, condition);
            }
        }
    }
}
