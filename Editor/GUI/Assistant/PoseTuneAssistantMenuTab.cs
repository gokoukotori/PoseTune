using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantMenuTab
    {
        public static void Draw(PoseTuneRoot root)
        {
            var menu = root.GetComponentInChildren<PoseMenu>(true);
            if (menu == null && GUILayout.Button("ポーズメニューを追加"))
            {
                var go = new GameObject("メニュー");
                Undo.RegisterCreatedObjectUndo(go, "PoseTune メニューを追加");
                go.transform.SetParent(root.transform, false);
                menu = go.AddComponent<PoseMenu>();
            }

            if (menu != null)
            {
                var editor = UnityEditor.Editor.CreateEditor(menu);
                editor.OnInspectorGUI();
                UnityEngine.Object.DestroyImmediate(editor);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用予定パラメータ", EditorStyles.boldLabel);
            new ParameterPlanPreviewRenderer().Draw(root);
        }
    }
}
