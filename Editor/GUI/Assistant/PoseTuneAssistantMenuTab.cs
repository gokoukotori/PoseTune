using System;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneAssistantMenuTab : IDisposable
    {
        private readonly ParameterPlanPreviewRenderer _parameterPlanPreview = new ParameterPlanPreviewRenderer();
        private UnityEditor.Editor _menuEditor;

        public void Draw(PoseTuneRoot root)
        {
            var menu = root.GetComponentInChildren<PoseMenu>(true);
            if (menu == null && GUILayout.Button("ポーズメニューを追加"))
            {
                var go = new GameObject("メニュー");
                Undo.RegisterCreatedObjectUndo(go, "PoseTune メニューを追加");
                go.transform.SetParent(root.transform, false);
                menu = go.AddComponent<PoseMenu>();
                _parameterPlanPreview.Invalidate();
            }

            if (menu != null)
            {
                UnityEditor.Editor.CreateCachedEditor(menu, null, ref _menuEditor);
                EditorGUI.BeginChangeCheck();
                _menuEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    _parameterPlanPreview.Invalidate();
                }
            }
            else
            {
                ReleaseMenuEditor();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("使用予定パラメータ", EditorStyles.boldLabel);
                if (GUILayout.Button("更新", EditorStyles.miniButton, GUILayout.Width(48)))
                {
                    _parameterPlanPreview.Invalidate();
                }
            }

            _parameterPlanPreview.Draw(root);
        }

        public void InvalidateParameterPreview()
        {
            _parameterPlanPreview.Invalidate();
        }

        public void Dispose()
        {
            ReleaseMenuEditor();
            _parameterPlanPreview.Dispose();
        }

        private void ReleaseMenuEditor()
        {
            if (_menuEditor == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_menuEditor);
            _menuEditor = null;
        }
    }
}
