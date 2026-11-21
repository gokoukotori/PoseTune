using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantRootSettingsPanel
    {
        internal static readonly PoseTuneFieldLabel[] SelectionFields =
        {
            new("parameterNamespace", "パラメータ名前空間"),
            new("targetLayer", "対象レイヤー"),
            new("enableAutoContextSwitch", "自動コンテキスト切替"),
            new("defaultMode", "既定モード"),
            new("poseSelectionSyncMode", "ポーズ同期方式")
        };

        internal static readonly PoseTuneFieldLabel[] OutputFields =
        {
            new("poseWriteDefaultsMode", "Write Defaults"),
            new("advancedSettings.keepGeneratedObjectsInBuild", "生成オブジェクトを Build に残す")
        };

        public static void Draw(SerializedObject rootObject)
        {
            PoseTuneInspectorGui.DrawFields(rootObject, SelectionFields);
            if (((PoseTuneRoot)rootObject.targetObject).poseSelectionSyncMode == PoseSelectionSyncMode.SharedExclusivePoseId)
            {
                EditorGUILayout.HelpBox(
                    "対象exclusiveグループをSaved属性ごとの共有Intへ圧縮します。Auto SelectedPosePerGroup、明示parameter名、非同期、non-exclusiveは専用Intへフォールバックします。",
                    MessageType.Info);
            }

            PoseTuneInspectorGui.DrawFields(rootObject, OutputFields);
        }
    }
}
