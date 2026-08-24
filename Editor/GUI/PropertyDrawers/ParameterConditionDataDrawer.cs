using System.Collections.Generic;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(ParameterConditionData))]
    public sealed class ParameterConditionDataDrawer : PropertyDrawer
    {
        private static readonly GUIContent ParameterLabel = new("パラメータ");
        private static readonly GUIContent ValueTypeLabel = new("値の型");
        private static readonly GUIContent OperatorLabel = new("比較");
        private static readonly GUIContent FloatValueLabel = new("Float 値");
        private static readonly GUIContent IntValueLabel = new("Int 値");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = FirstLine(position);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var parameter = property.FindPropertyRelative("parameter");
                var valueType = property.FindPropertyRelative("valueType");
                var op = property.FindPropertyRelative("op");

                line = NextLine(line);
                EditorGUI.PropertyField(line, parameter, ParameterLabel);

                line = NextLine(line);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(line, valueType, ValueTypeLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    NormalizeOperatorAfterValueTypeChange(valueType, op);
                }

                line = NextLine(line);
                DrawOperatorPopup(line, valueType, op);

                var valueProperty = ValueProperty(property, valueType, op, out var valueLabel);
                if (valueProperty != null)
                {
                    line = NextLine(line);
                    EditorGUI.PropertyField(line, valueProperty, valueLabel);
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

            var valueType = property.FindPropertyRelative("valueType");
            var op = property.FindPropertyRelative("op");
            var childLineCount = ValueProperty(property, valueType, op, out _) != null ? 4 : 3;
            return height + childLineCount *
                (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        private static void DrawOperatorPopup(
            Rect position,
            SerializedProperty valueTypeProperty,
            SerializedProperty operatorProperty)
        {
            var valueType = (ParameterValueType)valueTypeProperty.intValue;
            var current = (ConditionOperator)operatorProperty.intValue;
            var allowed = PoseTuneConditionRule.AllowedOperators(valueType);
            var currentIndex = IndexOf(allowed, current);
            var labels = new List<GUIContent>();
            if (currentIndex < 0)
            {
                labels.Add(new GUIContent($"無効 ({operatorProperty.intValue})"));
            }

            foreach (var op in allowed)
            {
                labels.Add(new GUIContent(OperatorText(op)));
            }

            if (labels.Count == 0)
            {
                EditorGUI.PropertyField(position, operatorProperty, OperatorLabel);
                return;
            }

            var selectedIndex = currentIndex < 0 ? 0 : currentIndex;
            if (currentIndex < 0)
            {
                selectedIndex = 0;
            }

            EditorGUI.showMixedValue = operatorProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUI.Popup(position, OperatorLabel, selectedIndex, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                var allowedIndex = currentIndex < 0 ? nextIndex - 1 : nextIndex;
                if (allowedIndex >= 0 && allowedIndex < allowed.Count)
                {
                    operatorProperty.intValue = (int)allowed[allowedIndex];
                }
            }

            EditorGUI.showMixedValue = false;
        }

        private static void NormalizeOperatorAfterValueTypeChange(
            SerializedProperty valueTypeProperty,
            SerializedProperty operatorProperty)
        {
            var valueType = (ParameterValueType)valueTypeProperty.intValue;
            var op = (ConditionOperator)operatorProperty.intValue;
            if (!PoseTuneConditionRule.IsAllowed(valueType, op))
            {
                operatorProperty.intValue = (int)PoseTuneConditionRule.DefaultOperator(valueType);
            }
        }

        private static SerializedProperty ValueProperty(
            SerializedProperty property,
            SerializedProperty valueTypeProperty,
            SerializedProperty operatorProperty,
            out GUIContent label)
        {
            var valueType = (ParameterValueType)valueTypeProperty.intValue;
            var op = (ConditionOperator)operatorProperty.intValue;
            switch (valueType)
            {
                case ParameterValueType.Int:
                    label = IntValueLabel;
                    return property.FindPropertyRelative("intValue");
                case ParameterValueType.Float:
                    label = FloatValueLabel;
                    return property.FindPropertyRelative("floatValue");
                default:
                    label = null;
                    return null;
            }
        }

        private static int IndexOf(IReadOnlyList<ConditionOperator> values, ConditionOperator value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string OperatorText(ConditionOperator op)
        {
            return op switch
            {
                ConditionOperator.Equals => "等しい",
                ConditionOperator.NotEquals => "等しくない",
                ConditionOperator.Greater => "より大きい",
                ConditionOperator.Less => "より小さい",
                ConditionOperator.GreaterOrEqual => "以上",
                ConditionOperator.LessOrEqual => "以下",
                ConditionOperator.If => "True の場合",
                ConditionOperator.IfNot => "False の場合",
                _ => op.ToString()
            };
        }

        private static Rect FirstLine(Rect position)
        {
            return new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        }

        private static Rect NextLine(Rect previous)
        {
            return new Rect(
                previous.x,
                previous.yMax + EditorGUIUtility.standardVerticalSpacing,
                previous.width,
                EditorGUIUtility.singleLineHeight);
        }
    }
}
