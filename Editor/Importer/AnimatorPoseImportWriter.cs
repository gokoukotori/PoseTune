using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorPoseImportWriter
    {
        public static List<PoseClip> ImportSelected(
            PoseTuneRoot root,
            IEnumerable<ImportCandidate> candidates)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var imported = new List<PoseClip>();
            var candidateList = (candidates ?? Enumerable.Empty<ImportCandidate>()).ToList();
            if (candidateList.Count > 0)
            {
                root.targetLayer = ToPoseTuneTargetLayer(candidateList[0].Target);
                EditorUtility.SetDirty(root);
            }

            foreach (var candidate in candidateList)
            {
                if (candidate.Clip == null)
                {
                    continue;
                }

                var group = FindOrCreateGroup(root, candidate.GroupKind);
                var pose = PoseTuneAuthoringFactory.AddPoseClip(group, candidate.Clip);
                pose.displayName = string.IsNullOrWhiteSpace(candidate.DisplayName)
                    ? ObjectNames.NicifyVariableName(candidate.Clip.name)
                    : candidate.DisplayName;
                pose.emitTrackingControl = candidate.HasTrackingBehavior;
                if (candidate.HasTrackingBehavior)
                {
                    var policy = Undo.AddComponent<PoseTrackingPolicy>(pose.gameObject);
                    policy.tracking = TrackingPolicyUtility.Copy(candidate.TrackingPolicy);
                    EditorUtility.SetDirty(policy);
                }
                var branches = candidate.ConditionBranches?
                    .Where(branch => branch != null)
                    .Select(branch => branch.Select(PoseTuneConditionUtility.Copy).ToList())
                    .ToList() ?? new List<List<ParameterConditionData>>();
                if (branches.Any(branch => branch.Count == 0))
                {
                    pose.clipConditions.Clear();
                }
                else if (branches.Count > 1)
                {
                    pose.clipConditions.Clear();
                    foreach (var branch in branches)
                    {
                        var condition = pose.gameObject.AddComponent<PoseCondition>();
                        condition.composition = ConditionComposition.And;
                        condition.conditions = branch;
                    }
                }
                else
                {
                    pose.clipConditions = (branches.Count == 1 ? branches[0] : candidate.Conditions.Select(PoseTuneConditionUtility.Copy))
                        .ToList();
                }

                imported.Add(pose);
            }

            return imported;
        }

        private static PoseTuneTargetLayer ToPoseTuneTargetLayer(PoseImportTarget target)
        {
            return target == PoseImportTarget.ActionLayer
                ? PoseTuneTargetLayer.Action
                : PoseTuneTargetLayer.Base;
        }

        private static PoseGroup FindOrCreateGroup(PoseTuneRoot root, PoseGroupKind kind)
        {
            var existing = root.GetComponentsInChildren<PoseGroup>(true)
                .FirstOrDefault(group => group.kind == kind);
            return existing != null ? existing : PoseTuneAuthoringFactory.AddPoseGroup(root, kind);
        }
    }
}
