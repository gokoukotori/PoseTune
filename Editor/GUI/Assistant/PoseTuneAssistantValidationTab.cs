using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantValidationTab
    {
        private static readonly Dictionary<int, bool> ShowAssetWriteFixes = new();

        public static void Draw(PoseTuneRoot root)
        {
            var graph = new PoseGraphCollector().Collect(root);
            new PoseTuneIconResolver().Apply(graph);
            var report = new PoseValidator().Validate(graph);
            var registry = new PoseTuneAutoFixRegistry();
            if (!report.Issues.Any())
            {
                EditorGUILayout.HelpBox("検証に成功しました。", MessageType.Info);
            }

            var id = root.GetInstanceID();
            ShowAssetWriteFixes.TryGetValue(id, out var showAssetWriteFixes);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("安全な修正を一括適用"))
                {
                    foreach (var issue in report.Issues.ToArray())
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

                showAssetWriteFixes = EditorGUILayout.ToggleLeft("Asset 書き込みを含む修正も表示", showAssetWriteFixes);
                ShowAssetWriteFixes[id] = showAssetWriteFixes;
            }

            foreach (var issue in report.Issues)
            {
                EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}",
                    issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
                foreach (var fix in registry.FindFixes(issue, graph))
                {
                    if (fix.Safety == AutoFixSafety.RequiresAssetWrite && !showAssetWriteFixes)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button($"Fix: {fix.Label} ({fix.Safety})"))
                        {
                            fix.Apply(issue, graph);
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
    }
}
