using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTuneAssistant))]
    public sealed class PoseTuneAssistantEditor : PoseTuneLocalizedEditor
    {
        private const int PoseTabIndex = 0;
        private const int MenuTabIndex = 1;
        private const int TrackingTabIndex = 2;
        private const int HeightTabIndex = 3;
        private const int PreviewTabIndex = 4;
        private const int ValidationTabIndex = 5;

        private PoseTuneAssistantMenuTab _menuTab;
        private PoseTuneAssistantValidationTab _validationTab;
        private SerializedObject _rootObject;

        // Order must match the *TabIndex constants above.
        private static readonly string[] Tabs =
        {
            "ポーズ", "メニュー", "トラッキング", "高さ", "プレビュー", "検証"
        };

        private void OnEnable()
        {
            _menuTab = new PoseTuneAssistantMenuTab();
            _validationTab = new PoseTuneAssistantValidationTab();
        }

        private void OnDisable()
        {
            _menuTab?.Dispose();
            _menuTab = null;
            _validationTab?.Dispose();
            _validationTab = null;
            _rootObject?.Dispose();
            _rootObject = null;
        }

        // The tabs edit the parent PoseTuneRoot, not the inspected assistant, so they get their own
        // SerializedObject for it (for Undo and prefab overrides).
        private SerializedObject RootObject(PoseTuneRoot root)
        {
            if (_rootObject == null || _rootObject.targetObject != root)
            {
                _rootObject?.Dispose();
                _rootObject = new SerializedObject(root);
            }

            return _rootObject;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var assistant = (PoseTuneAssistant)target;
            var root = assistant.GetComponentInParent<PoseTuneRoot>(true);
            if (root == null)
            {
                EditorGUILayout.HelpBox("PoseTuneRoot が見つかりません。テンプレートから作成し直してください。", MessageType.Error);
                return;
            }

            assistant.lastSelectedTab = GUILayout.Toolbar(Mathf.Clamp(assistant.lastSelectedTab, 0, Tabs.Length - 1), Tabs);
            EditorGUILayout.Space();

            var rootObject = RootObject(root);
            switch (assistant.lastSelectedTab)
            {
                case PoseTabIndex:
                    PoseTuneAssistantPoseTab.Draw(root);
                    break;
                case MenuTabIndex:
                    (_menuTab ??= new PoseTuneAssistantMenuTab()).Draw(root);
                    break;
                case TrackingTabIndex:
                    PoseTuneAssistantTrackingTab.Draw(root, rootObject);
                    break;
                case HeightTabIndex:
                    PoseTuneAssistantHeightTab.Draw(root, rootObject);
                    break;
                case PreviewTabIndex:
                    PoseTuneAssistantPreviewTab.Draw(root);
                    break;
                case ValidationTabIndex:
                    (_validationTab ??= new PoseTuneAssistantValidationTab()).Draw(root);
                    break;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            assistant.showAdvanced = EditorGUILayout.Foldout(assistant.showAdvanced, "詳細設定");
            if (assistant.showAdvanced)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    PoseTuneAssistantRootSettingsPanel.Draw(rootObject);
                }
            }
            var advancedSettingsChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                if (assistant.lastSelectedTab != MenuTabIndex || advancedSettingsChanged)
                {
                    _menuTab?.InvalidateParameterPreview();
                }

                if (assistant.lastSelectedTab != ValidationTabIndex || advancedSettingsChanged)
                {
                    _validationTab?.Invalidate();
                }

                EditorUtility.SetDirty(assistant);
                EditorUtility.SetDirty(root);
            }
        }
    }

}
