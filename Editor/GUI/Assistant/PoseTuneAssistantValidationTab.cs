using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantValidationTab
    {
        private static readonly Dictionary<string, bool> GroupFoldouts = new();

        public static void Draw(PoseTuneRoot root)
        {
            var graph = new PoseGraphCollector().Collect(root);
            new PoseTuneIconResolver().Apply(graph);
            var report = new PoseValidator().Validate(graph);
            var registry = new PoseTuneAutoFixRegistry();
            var groups = ValidationIssueGrouping.Group(report.Issues);
            if (!report.Errors.Any() && !report.Warnings.Any())
            {
                EditorGUILayout.HelpBox("検証に成功しました。", MessageType.Info);
            }

            var id = root.GetInstanceID();
            if (GUILayout.Button("安全な修正を一括適用"))
            {
                foreach (var issue in groups.SelectMany(group => group.Issues).ToArray())
                {
                    foreach (var fix in registry.FindFixes(issue, graph)
                                 .Where(fix => fix.IncludeInBatch &&
                                               (fix.Safety == AutoFixSafety.Safe ||
                                                fix.Safety == AutoFixSafety.Reversible)))
                    {
                        fix.Apply(issue, graph);
                    }
                }

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
                                EditorGUILayout.ObjectField(context, typeof(Object), true);
                            }
                        }
                    }
                }

                foreach (var fix in CommonFixes(group, registry, graph))
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

                            GUI.changed = true;
                            return;
                        }
                    }
                }
            }

            if (GUILayout.Button("明示指定されたグループパラメータをクリア"))
            {
                foreach (var group in root.GetComponentsInChildren<PoseGroup>(true))
                {
                    if (!string.IsNullOrWhiteSpace(group.parameterName))
                    {
                        group.parameterName = "";
                        EditorUtility.SetDirty(group);
                    }
                }
            }
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
