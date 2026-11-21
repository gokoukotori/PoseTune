using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneAssistantValidationSnapshot
    {
        public PoseTuneAssistantValidationSnapshot(
            PoseGraph graph,
            ValidationReport report,
            IReadOnlyList<ValidationIssueGroup> groups)
        {
            Graph = graph;
            Report = report;
            Groups = groups;
        }

        public PoseGraph Graph { get; }
        public ValidationReport Report { get; }
        public IReadOnlyList<ValidationIssueGroup> Groups { get; }
    }

    internal sealed class PoseTuneAssistantValidationTab : IDisposable
    {
        private static readonly Dictionary<string, bool> GroupFoldouts = new();
        private readonly PoseTuneAutoFixRegistry _registry = new PoseTuneAutoFixRegistry();
        private readonly PoseTuneRootSnapshotCache<PoseTuneAssistantValidationSnapshot> _cache;

        public PoseTuneAssistantValidationTab()
            : this(CreateSnapshot)
        {
        }

        internal PoseTuneAssistantValidationTab(
            Func<PoseTuneRoot, PoseTuneAssistantValidationSnapshot> createSnapshot)
        {
            _cache = new PoseTuneRootSnapshotCache<PoseTuneAssistantValidationSnapshot>(createSnapshot);
        }

        public void Draw(PoseTuneRoot root)
        {
            if (GUILayout.Button("再検証"))
            {
                Invalidate();
            }

            var snapshot = GetSnapshot(root);
            var graph = snapshot.Graph;
            var report = snapshot.Report;
            var groups = snapshot.Groups;
            if (!report.Errors.Any() && !report.Warnings.Any())
            {
                EditorGUILayout.HelpBox("検証に成功しました。", MessageType.Info);
            }

            var id = root.GetInstanceID();
            if (GUILayout.Button("安全な修正を一括適用"))
            {
                foreach (var issue in groups.SelectMany(group => group.Issues).ToArray())
                {
                    foreach (var fix in _registry.FindFixes(issue, graph)
                                 .Where(fix => fix.IncludeInBatch &&
                                               (fix.Safety == AutoFixSafety.Safe ||
                                                fix.Safety == AutoFixSafety.Reversible)))
                    {
                        fix.Apply(issue, graph);
                    }
                }

                Invalidate();
                GUI.changed = true;
                return;
            }

            foreach (var group in groups)
            {
                var targetSuffix = group.TargetCount > 1 ? $"（対象 {group.TargetCount}件）" : "";
                EditorGUILayout.HelpBox($"{group.Code}: {group.Message}{targetSuffix}", MessageTypeFor(group.Severity));

                if (group.TargetCount > 1)
                {
                    var foldoutKey = $"{id}|{group.Severity}|{group.Code}|{group.Message}";
                    GroupFoldouts.TryGetValue(foldoutKey, out var expanded);
                    expanded = EditorGUILayout.Foldout(expanded, "対象を表示", true);
                    GroupFoldouts[foldoutKey] = expanded;
                    if (expanded)
                    {
                        foreach (var context in group.Contexts)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20);
                                EditorGUILayout.ObjectField(context, typeof(UnityEngine.Object), true);
                            }
                        }
                    }
                }

                foreach (var fix in CommonFixes(group, _registry, graph))
                {
                    var prefix = group.TargetCount > 1 ? "Fix all" : "Fix";
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button($"{prefix}: {fix.Label} ({fix.Safety})"))
                        {
                            foreach (var issue in group.Issues)
                            {
                                fix.Apply(issue, graph);
                            }

                            Invalidate();
                            GUI.changed = true;
                            return;
                        }
                    }
                }
            }
        }

        public void Invalidate()
        {
            _cache.Invalidate();
        }

        public void Dispose()
        {
            _cache.Dispose();
        }

        internal PoseTuneAssistantValidationSnapshot GetSnapshot(PoseTuneRoot root)
        {
            return _cache.Get(root);
        }

        private static PoseTuneAssistantValidationSnapshot CreateSnapshot(PoseTuneRoot root)
        {
            var graph = new PoseGraphCollector().Collect(root);
            new PoseTuneIconResolver().Apply(graph);
            var report = new PoseValidator().Validate(graph);
            return new PoseTuneAssistantValidationSnapshot(
                graph,
                report,
                ValidationIssueGrouping.Group(report.Issues));
        }

        private static IEnumerable<IPoseTuneAutoFix> CommonFixes(
            ValidationIssueGroup group,
            PoseTuneAutoFixRegistry registry,
            PoseGraph graph)
        {
            List<IPoseTuneAutoFix> common = null;
            foreach (var issue in group.Issues)
            {
                var fixes = registry.FindFixes(issue, graph).ToList();
                common = common == null ? fixes : common.Intersect(fixes).ToList();
            }

            return common ?? Enumerable.Empty<IPoseTuneAutoFix>();
        }

        private static MessageType MessageTypeFor(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error:
                    return MessageType.Error;
                case ValidationSeverity.Warning:
                    return MessageType.Warning;
                case ValidationSeverity.Information:
                    return MessageType.Info;
                default:
                    return MessageType.None;
            }
        }
    }
}
