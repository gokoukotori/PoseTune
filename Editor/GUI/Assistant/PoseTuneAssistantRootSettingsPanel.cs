using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantRootSettingsPanel
    {
        public static void Draw(PoseTuneRoot root)
        {
            root.displayName = EditorGUILayout.TextField("表示名", root.displayName);
            root.parameterNamespace = EditorGUILayout.TextField("パラメータ名前空間", root.parameterNamespace);
            root.targetLayer = (PoseTuneTargetLayer)EditorGUILayout.EnumPopup("対象レイヤー", root.targetLayer);
            root.enableAutoContextSwitch = EditorGUILayout.Toggle("自動コンテキスト切替", root.enableAutoContextSwitch);
            root.defaultMode = (PoseTuneDefaultMode)EditorGUILayout.EnumPopup("既定モード", root.defaultMode);
            root.poseSelectionSyncMode = (PoseSelectionSyncMode)EditorGUILayout.EnumPopup("ポーズ同期方式", root.poseSelectionSyncMode);
            root.poseWriteDefaultsMode = (PoseWriteDefaultsMode)EditorGUILayout.EnumPopup("Write Defaults", root.poseWriteDefaultsMode);
            root.advancedSettings.keepGeneratedObjectsInBuild =
                EditorGUILayout.Toggle("生成オブジェクトを Build に残す", root.advancedSettings.keepGeneratedObjectsInBuild);
        }
    }
}
