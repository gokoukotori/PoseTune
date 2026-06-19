using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public readonly struct PoseTuneFieldLabel
    {
        public readonly string Path;
        public readonly GUIContent Label;

        public PoseTuneFieldLabel(string path, string label, string tooltip = "")
        {
            Path = path;
            Label = new GUIContent(label, tooltip);
        }
    }

    internal static class PoseTuneInspectorGui
    {
        public static GUIContent Content(string label, string tooltip = "")
        {
            return new GUIContent(label, tooltip);
        }
    }

    public abstract class PoseTuneLocalizedEditor : UnityEditor.Editor
    {
        protected void Draw(string propertyPath, string label, string tooltip = "")
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, PoseTuneInspectorGui.Content(label, tooltip), true);
            }
        }

        protected void DrawFields(params PoseTuneFieldLabel[] fields)
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                Draw("m_Script", "スクリプト");
            }

            foreach (var field in fields)
            {
                Draw(field.Path, field.Label.text, field.Label.tooltip);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
