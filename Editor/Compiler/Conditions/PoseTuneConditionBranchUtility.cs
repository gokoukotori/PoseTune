using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor.Compiler.Conditions
{
    internal static class PoseTuneConditionBranchUtility
    {
        public static List<List<ParameterConditionData>> TrueBranches()
        {
            return new List<List<ParameterConditionData>> { new() };
        }

        public static List<List<ParameterConditionData>> FromPoseCondition(PoseCondition condition)
        {
            if (condition == null || condition.conditions == null || condition.conditions.Count == 0)
            {
                return TrueBranches();
            }

            if (condition.composition == ConditionComposition.Or)
            {
                return condition.conditions
                    .Select(item => new List<ParameterConditionData> { item })
                    .ToList();
            }

            return new List<List<ParameterConditionData>>
            {
                new(condition.conditions)
            };
        }

        public static List<List<ParameterConditionData>> FromPoseConditions(IEnumerable<PoseCondition> conditions)
        {
            var conditionList = conditions?.ToList() ?? new List<PoseCondition>();
            if (conditionList.Count == 0)
            {
                return TrueBranches();
            }

            var branches = new List<List<ParameterConditionData>>();
            foreach (var condition in conditionList)
            {
                branches.AddRange(FromPoseCondition(condition));
            }

            return branches.Count == 0 ? TrueBranches() : branches;
        }

        public static List<List<ParameterConditionData>> AndConditions(
            List<List<ParameterConditionData>> branches,
            IEnumerable<ParameterConditionData> conditions)
        {
            var branchList = branches ?? TrueBranches();
            var conditionList = conditions?.ToList() ?? new List<ParameterConditionData>();
            if (conditionList.Count == 0)
            {
                return Clone(branchList);
            }

            return branchList.Select(branch =>
            {
                var next = new List<ParameterConditionData>(branch);
                next.AddRange(conditionList);
                return next;
            }).ToList();
        }

        public static List<List<ParameterConditionData>> AndBranches(
            List<List<ParameterConditionData>> left,
            List<List<ParameterConditionData>> right)
        {
            var leftBranches = left ?? TrueBranches();
            var rightBranches = right ?? TrueBranches();
            if (rightBranches.Count == 0)
            {
                return Clone(leftBranches);
            }

            var result = new List<List<ParameterConditionData>>();
            foreach (var leftBranch in leftBranches)
            {
                foreach (var rightBranch in rightBranches)
                {
                    var branch = new List<ParameterConditionData>(leftBranch);
                    branch.AddRange(rightBranch);
                    result.Add(branch);
                }
            }

            return result.Count == 0 ? TrueBranches() : result;
        }

        public static List<List<ParameterConditionData>> Clone(List<List<ParameterConditionData>> branches)
        {
            return (branches ?? TrueBranches())
                .Select(branch => new List<ParameterConditionData>(branch))
                .ToList();
        }
    }
}
