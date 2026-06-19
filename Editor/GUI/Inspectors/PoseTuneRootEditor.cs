using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTuneRoot))]
    [CanEditMultipleObjects]
    public sealed class PoseTuneRootEditor : PoseTuneLocalizedEditor
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("displayName", "表示名"),
            new("parameterNamespace", "パラメータ名前空間"),
            new("targetLayer", "対象レイヤー"),
            new("enableAutoContextSwitch", "自動コンテキスト切替"),
            new("enableHeightAdjust", "高さ調整を有効化"),
            new("enableIconGeneration", "アイコン生成を有効化"),
            new("questLowMemoryMode", "Quest / 低メモリモード"),
            new("disableWhenFullBodyTracking", "FBT 時に無効化"),
            new("previewSettings", "プレビュー設定"),
            new("advancedSettings", "詳細設定")
        };

        public override void OnInspectorGUI()
        {
            DrawFields(FieldLabels);

            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var root = (PoseTuneRoot)target;
            if (GUILayout.Button("アシスタントを開く"))
            {
                var assistant = root.GetComponentInChildren<PoseTuneAssistant>(true) ?? root.gameObject.AddComponent<PoseTuneAssistant>();
                Selection.activeObject = assistant;
            }
        }
    }
}
