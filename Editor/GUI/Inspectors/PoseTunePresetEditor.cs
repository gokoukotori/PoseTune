using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTunePreset))]
    [CanEditMultipleObjects]
    public sealed class PoseTunePresetEditor : PoseTuneLocalizedEditor
    {
        [MenuItem("Assets/Create/PoseTune/PoseTune プリセット", false, 110)]
        private static void CreatePresetAsset()
        {
            ProjectWindowUtil.CreateAsset(CreateCurrentPreset(), "New PoseTune Preset.asset");
        }

        internal static PoseTunePreset CreateCurrentPreset()
        {
            var preset = ScriptableObject.CreateInstance<PoseTunePreset>();
            preset.schemaVersion = PoseTunePreset.CurrentSchemaVersion;
            return preset;
        }

        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("presetName", "プリセット名"),
                new PoseTuneFieldLabel("rootTrackingPolicy", "Root トラッキングポリシー"),
                new PoseTuneFieldLabel("groups", "グループ"),
                new PoseTuneFieldLabel("menu", "メニュー"),
                new PoseTuneFieldLabel("height", "高さ"));
        }
    }
}
