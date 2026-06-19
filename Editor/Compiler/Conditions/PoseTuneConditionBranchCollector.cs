using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Conditions
{
    internal static class PoseTuneConditionBranchCollector
    {
        public static List<List<ParameterConditionData>> Collect(PoseGraph graph, PoseGroup group, PoseClip clip)
        {
            var branches = CollectConditionBranches(group.transform, clip, group.groupConditions);
            return ApplyGoroneSystemExGuard(graph, group, branches);
        }

        private static List<List<ParameterConditionData>> CollectConditionBranches(
            Transform group,
            PoseClip clip,
            IEnumerable<ParameterConditionData> groupConditions)
        {
            var result = PoseTuneConditionBranchUtility.AndConditions(
                PoseTuneConditionBranchUtility.TrueBranches(),
                groupConditions);
            var chain = new List<Transform>();
            var cursor = clip.transform;
            while (cursor != null)
            {
                chain.Add(cursor);
                if (cursor == group)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            chain.Reverse();
            foreach (var t in chain)
            {
                result = PoseTuneConditionBranchUtility.AndBranches(result, CollectLocalConditionBranches(t));
            }

            return PoseTuneConditionBranchUtility.AndConditions(result, clip.clipConditions);
        }

        private static List<List<ParameterConditionData>> ApplyGoroneSystemExGuard(
            PoseGraph graph,
            PoseGroup group,
            List<List<ParameterConditionData>> branches)
        {
            if (!ShouldApplyGoroneSystemExGuard(graph, group))
            {
                return PoseTuneConditionBranchUtility.Clone(branches);
            }

            return branches.Select(branch =>
            {
                var guarded = new List<ParameterConditionData>(branch);
                if (!guarded.Any(condition => condition.parameter == GoroneSystemExDetector.VrcSupineParameter))
                {
                    guarded.Add(VrcSupineInactiveCondition());
                }

                return guarded;
            }).ToList();
        }

        private static bool ShouldApplyGoroneSystemExGuard(PoseGraph graph, PoseGroup group)
        {
            var compatibility = graph?.GoroneSystemExCompatibility;
            if (compatibility == null || group == null)
            {
                return false;
            }

            switch (compatibility.guardMode)
            {
                case GoroneSystemExGuardMode.AllPoseGroups:
                    return true;
                case GoroneSystemExGuardMode.LowerBodyPoseGroups:
                    return group.kind == PoseGroupKind.Chair ||
                           group.kind == PoseGroupKind.Floor ||
                           group.kind == PoseGroupKind.Prone ||
                           group.kind == PoseGroupKind.Supine;
                default:
                    return false;
            }
        }

        private static ParameterConditionData VrcSupineInactiveCondition()
        {
            return new ParameterConditionData
            {
                parameter = GoroneSystemExDetector.VrcSupineParameter,
                valueType = ParameterValueType.Int,
                op = ConditionOperator.Equals,
                intValue = 0
            };
        }

        private static List<List<ParameterConditionData>> CollectLocalConditionBranches(Transform transform)
        {
            var conditions = transform.GetComponents<PoseCondition>()
                .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                .ToArray();
            if (conditions.Length == 0)
            {
                return PoseTuneConditionBranchUtility.TrueBranches();
            }

            return PoseTuneConditionBranchUtility.FromPoseConditions(conditions);
        }
    }
}
