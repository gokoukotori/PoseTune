using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTuneAssistant))]
    public sealed class PoseTuneAssistantEditor : UnityEditor.Editor
    {
        private static readonly string[] Tabs =
        {
            "ポーズ", "メニュー", "トラッキング", "高さ", "インポート", "プレビュー", "プリセット", "検証"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var assistant = (PoseTuneAssistant)target;
            var root = assistant.GetComponentInParent<PoseTuneRoot>(true);
            if (root == null)
            {
                EditorGUILayout.HelpBox("PoseTuneRoot が見つかりません。テンプレートから作成し直してください。", MessageType.Error);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), new GUIContent("スクリプト"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("lastSelectedTab"), new GUIContent("最後に選択したタブ"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showAdvanced"), new GUIContent("詳細設定を表示"));
                serializedObject.ApplyModifiedProperties();
                return;
            }

            assistant.lastSelectedTab = GUILayout.Toolbar(Mathf.Clamp(assistant.lastSelectedTab, 0, Tabs.Length - 1), Tabs);
            EditorGUILayout.Space();

            switch (assistant.lastSelectedTab)
            {
                case 0:
                    PoseTuneAssistantPoseTab.Draw(root);
                    break;
                case 1:
                    PoseTuneAssistantMenuTab.Draw(root);
                    break;
                case 2:
                    PoseTuneAssistantTrackingTab.Draw(root);
                    break;
                case 3:
                    PoseTuneAssistantHeightTab.Draw(root);
                    break;
                case 4:
                    PoseTuneAssistantImportTab.Draw(root);
                    break;
                case 5:
                    PoseTuneAssistantPreviewTab.Draw(root);
                    break;
                case 6:
                    PoseTuneAssistantPresetTab.Draw(root);
                    break;
                case 7:
                    PoseTuneAssistantValidationTab.Draw(root);
                    break;
            }

            EditorGUILayout.Space();
            assistant.showAdvanced = EditorGUILayout.Foldout(assistant.showAdvanced, "詳細設定");
            if (assistant.showAdvanced)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    PoseTuneAssistantRootSettingsPanel.Draw(root);
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(assistant);
                EditorUtility.SetDirty(root);
            }
        }
    }

}
