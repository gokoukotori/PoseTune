using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public abstract class PoseTuneLocalizedPropertyDrawer : PropertyDrawer
    {
        protected abstract PoseTuneFieldLabel[] Fields { get; }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                foreach (var field in Fields)
                {
                    var child = property.FindPropertyRelative(field.Path);
                    if (child == null)
                    {
                        continue;
                    }

                    var height = EditorGUI.GetPropertyHeight(child, field.Label, true);
                    var rect = new Rect(position.x, y, position.width, height);
                    EditorGUI.PropertyField(rect, child, field.Label, true);
                    y += height + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            foreach (var field in Fields)
            {
                var child = property.FindPropertyRelative(field.Path);
                if (child == null)
                {
                    continue;
                }

                height += EditorGUI.GetPropertyHeight(child, field.Label, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
