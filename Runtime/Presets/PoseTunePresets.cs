using System.Collections.Generic;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [CreateAssetMenu(menuName = "PoseTune/PoseTune プリセット")]
    public sealed class PoseTunePreset : ScriptableObject
    {
        [InspectorName("プリセット名")]
        public string presetName = "";
        [InspectorName("グループ")]
        public List<PoseGroupPresetData> groups = new();
        [InspectorName("メニュー")]
        public PoseMenuPresetData menu = new();
        [InspectorName("高さ")]
        public PoseHeightPresetData height = new();
    }

    [CreateAssetMenu(menuName = "PoseTune/アバター調整プリセット")]
    public sealed class AvatarAdjustmentPreset : ScriptableObject
    {
        [InspectorName("アバター名")]
        public string avatarName = "";
        [InspectorName("アバターアセット GUID ハッシュ")]
        public string avatarAssetGuidHash = "";
        [InspectorName("調整")]
        public List<PoseAdjustmentEntry> adjustments = new();
    }
}
