using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTunePreset))]
    [CanEditMultipleObjects]
    public sealed class PoseTunePresetEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("presetName", "プリセット名"),
                new PoseTuneFieldLabel("groups", "グループ"),
                new PoseTuneFieldLabel("menu", "メニュー"),
                new PoseTuneFieldLabel("height", "高さ"));
        }
    }
}
