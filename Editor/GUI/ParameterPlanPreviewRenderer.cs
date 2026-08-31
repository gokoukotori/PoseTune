using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class ParameterPlanPreviewRow
    {
        public ParameterPlanPreviewRow(ParameterDefinition parameter)
        {
            Name = parameter.Name ?? "";
            ValueType = parameter.ValueType;
            SyncType = parameter.SyncType;
            LocalOnly = parameter.LocalOnly;
            Saved = parameter.Saved;
        }

        public string Name { get; }
        public PoseTuneParameterValueType ValueType { get; }
        public PoseTuneParameterSyncType SyncType { get; }
        public bool LocalOnly { get; }
        public bool Saved { get; }

        public string AttributesLabel
        {
            get
            {
                var attributes = new List<string>
                {
                    ValueType.ToString(),
                    SyncType == PoseTuneParameterSyncType.NotSynced ? "同期なし" : "同期"
                };
                if (LocalOnly)
                {
                    attributes.Add("ローカル");
                }

                if (Saved)
                {
                    attributes.Add("保存");
                }

                return string.Join(" / ", attributes);
            }
        }
    }

    internal sealed class ParameterPlanPreviewModel
    {
        private ParameterPlanPreviewModel(IReadOnlyList<ParameterPlanPreviewRow> rows)
        {
            Rows = rows;
        }

        public IReadOnlyList<ParameterPlanPreviewRow> Rows { get; }
        public int TotalCount => Rows.Count;

        public static ParameterPlanPreviewModel Create(ParameterPlan plan)
        {
            var parameters = plan != null
                ? plan.Parameters.AsEnumerable()
                : Enumerable.Empty<ParameterDefinition>();
            var rows = parameters
                .Where(parameter => parameter != null && !parameter.AnimatorOnly)
                .GroupBy(parameter => parameter.Name ?? "", StringComparer.Ordinal)
                .Select(group => new ParameterPlanPreviewRow(group.First()))
                .OrderBy(row => row.Name, StringComparer.Ordinal)
                .ToList();
            return new ParameterPlanPreviewModel(rows);
        }
    }

    internal sealed class ParameterPlanPreviewRenderer
    {
        public void Draw(PoseTuneRoot root)
        {
            if (root == null)
            {
                return;
            }

            var graph = new PoseGraphCollector().Collect(root);
            var report = new PoseValidator().Validate(graph);
            if (report.HasErrors)
            {
                foreach (var issue in report.Errors)
                {
                    EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}", MessageType.Error);
                }

                return;
            }

            ParameterPlan parameters;
            try
            {
                parameters = new ParameterAllocator().AllocateStrict(graph);
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
                return;
            }

            Draw(ParameterPlanPreviewModel.Create(parameters));
        }

        private static void Draw(ParameterPlanPreviewModel model)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Expression Parameters: {model.TotalCount}");
            }

            if (model.TotalCount == 0)
            {
                EditorGUILayout.HelpBox("使用予定パラメータはありません。", MessageType.Info);
                return;
            }

            foreach (var row in model.Rows)
            {
                DrawRow(row);
            }
        }

        private static void DrawRow(ParameterPlanPreviewRow row)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(row.Name, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField(row.AttributesLabel, GUILayout.MinWidth(150));
            }
        }
    }
}
