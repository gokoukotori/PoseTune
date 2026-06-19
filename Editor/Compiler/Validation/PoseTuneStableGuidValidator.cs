using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneStableGuidValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            if (graph.AvatarRoot != null)
            {
                ReportDuplicateStableGuids(
                    graph.AvatarRoot.GetComponentsInChildren<PoseTuneRoot>(true),
                    root => root.StableGuid,
                    root => root,
                    PoseTuneDiagnostics.DuplicateRootStableGuid.Code,
                    "PoseTuneRoot",
                    report);
            }

            ReportDuplicateStableGuids(
                graph.Groups.Where(group => group.Source != null),
                group => group.Id,
                group => group.Source,
                PoseTuneDiagnostics.DuplicateGroupStableGuid.Code,
                "PoseGroup",
                report);
            ReportDuplicateStableGuids(
                graph.Poses.Where(pose => pose.Source != null),
                pose => pose.Id,
                pose => pose.Source,
                PoseTuneDiagnostics.DuplicatePoseStableGuid.Code,
                "PoseClip",
                report);
        }

        private static void ReportDuplicateStableGuids<T>(
            IEnumerable<T> items,
            System.Func<T, string> stableGuid,
            System.Func<T, Object> context,
            string code,
            string componentName,
            ValidationReport report)
        {
            var candidates = items
                .Select(item => new
                {
                    Item = item,
                    StableGuid = stableGuid(item),
                    Context = context(item)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.StableGuid))
                .ToList();

            foreach (var duplicate in candidates.GroupBy(item => item.StableGuid).Where(group => group.Count() > 1))
            {
                foreach (var item in duplicate)
                {
                    report.Error(code, componentName + " stable GUID が重複しています: " + duplicate.Key, item.Context);
                }
            }
        }
    }
}
