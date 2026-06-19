using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(AvatarAdjustmentPreset))]
    [CanEditMultipleObjects]
    public sealed class AvatarAdjustmentPresetEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("avatarName", "アバター名"),
                new PoseTuneFieldLabel("avatarAssetGuidHash", "アバターアセット GUID ハッシュ"),
                new PoseTuneFieldLabel("adjustments", "調整"));
        }
    }
}
