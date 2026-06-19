using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneEntrySimulationService
    {
        private readonly PoseTuneConditionEvaluator evaluator = new();

        public bool AutoContextMatches(PoseTuneRoot root, PoseGroupKind kind, PoseTuneParameterSnapshot snapshot)
        {
            return evaluator.AllMatch(AutoContextConditions(root, kind), snapshot);
        }

        public bool AutoContextMatches(PoseTuneRoot root, PoseGroupDefinition group, PoseTuneParameterSnapshot snapshot)
        {
            return evaluator.AllMatch(AutoContextConditions(root, group), snapshot);
        }

        public bool PoseEntryMatches(PoseDefinition pose, PoseTuneParameterSnapshot snapshot)
        {
            if (pose == null)
            {
                return false;
            }

            return evaluator.AnyBranchMatches(pose.ConditionBranches, snapshot);
        }

        public IReadOnlyList<ParameterConditionData> AutoContextConditions(PoseTuneRoot root, PoseGroupKind kind)
        {
            return PoseTuneAutoContextConditionService.AutoContextConditions(
                root,
                kind,
                ResolveAutoPoseSelectionMode(root, kind),
                ResolveAutoContextProfile(root, kind));
        }

        public IReadOnlyList<ParameterConditionData> AutoContextConditions(
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            return PoseTuneAutoContextConditionService.AutoContextConditions(root, group);
        }

        private static AutoPoseSelectionMode ResolveAutoPoseSelectionMode(PoseTuneRoot root, PoseGroupKind kind)
        {
            if (root == null)
            {
                return AutoPoseSelectionMode.InitialPoseOnly;
            }

            return root.GetComponentsInChildren<PoseGroup>(true)
                .Where(PoseTuneAuthoringInclusion.Includes)
                .Where(group => group.kind == kind)
                .Select(group => group.autoPoseSelectionMode)
                .DefaultIfEmpty(AutoPoseSelectionMode.InitialPoseOnly)
                .Max();
        }

        private static AutoContextProfile ResolveAutoContextProfile(PoseTuneRoot root, PoseGroupKind kind)
        {
            if (root == null)
            {
                return AutoContextProfile.Standard;
            }

            return root.GetComponentsInChildren<PoseGroup>(true)
                .Where(PoseTuneAuthoringInclusion.Includes)
                .Where(group => group.kind == kind)
                .Select(group => group.autoContextProfile)
                .DefaultIfEmpty(AutoContextProfile.Standard)
                .Max();
        }
    }
}
