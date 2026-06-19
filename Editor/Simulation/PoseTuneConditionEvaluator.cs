using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneConditionEvaluator
    {
        public bool AnyBranchMatches(IEnumerable<List<ParameterConditionData>> branches, PoseTuneParameterSnapshot snapshot)
        {
            var branchList = (branches ?? Enumerable.Empty<List<ParameterConditionData>>()).ToList();
            if (branchList.Count == 0)
            {
                return true;
            }

            return branchList.Any(branch => AllMatch(branch, snapshot));
        }

        public bool AllMatch(IEnumerable<ParameterConditionData> conditions, PoseTuneParameterSnapshot snapshot)
        {
            return (conditions ?? Enumerable.Empty<ParameterConditionData>())
                .Where(condition => condition != null && !string.IsNullOrWhiteSpace(condition.parameter))
                .All(condition => Matches(condition, snapshot));
        }

        public bool Matches(ParameterConditionData condition, PoseTuneParameterSnapshot snapshot)
        {
            return PoseTuneConditionUtility.Matches(condition, snapshot);
        }
    }
}
