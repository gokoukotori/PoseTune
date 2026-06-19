using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class MenuPlanPreviewRow
    {
        public int Depth;
        public string Label = "";
        public PoseTuneMenuControlType Type;
        public string Parameter = "";
        public float Value;
        public int ChildCount;
        public Texture2D Icon;
    }

    internal sealed class MenuPlanPreviewRenderer
    {
        public void Draw(PoseTuneRoot root)
        {
            if (root == null)
            {
                return;
            }

            var graph = new PoseGraphCollector().Collect(root);
            new PoseTuneIconResolver().Apply(graph);
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
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
                return;
            }

            var menuPlan = new MenuCompiler().Compile(graph, parameters);
            foreach (var row in Flatten(menuPlan))
            {
                DrawRow(row);
            }
        }

        public static IEnumerable<MenuPlanPreviewRow> Flatten(MenuPlan plan)
        {
            if (plan?.Root == null)
            {
                yield break;
            }

            foreach (var row in Flatten(plan.Root, 0))
            {
                yield return row;
            }
        }

        private static IEnumerable<MenuPlanPreviewRow> Flatten(MenuControlPlan control, int depth)
        {
            if (control == null)
            {
                yield break;
            }

            yield return new MenuPlanPreviewRow
            {
                Depth = depth,
                Label = control.Label,
                Type = control.Type,
                Parameter = control.Parameter,
                Value = control.Value,
                ChildCount = control.Children.Count,
                Icon = control.Icon
            };

            foreach (var child in control.Children)
            {
                foreach (var row in Flatten(child, depth + 1))
                {
                    yield return row;
                }
            }
        }

        private static void DrawRow(MenuPlanPreviewRow row)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(row.Depth * 16f);
                EditorGUILayout.LabelField(row.Type.ToString(), GUILayout.Width(90));
                EditorGUILayout.LabelField(row.Label, GUILayout.MinWidth(140));
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(row.Parameter) ? "-" : row.Parameter, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField(row.Type == PoseTuneMenuControlType.SubMenu ? $"{row.ChildCount} controls" : row.Value.ToString("0.###"),
                    GUILayout.Width(90));
                EditorGUILayout.LabelField(row.Icon != null ? "icon" : "no icon", GUILayout.Width(60));
            }
        }
    }
}
