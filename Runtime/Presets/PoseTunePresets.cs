using System.Collections.Generic;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [CreateAssetMenu(menuName = "PoseTune/PoseTune プリセット")]
    public sealed class PoseTunePreset : ScriptableObject
    {
        public const int CurrentSchemaVersion = 2;

        [HideInInspector]
        // Assets created before schema v2 have no serialized version field and must remain legacy-safe.
        // Capture writes CurrentSchemaVersion explicitly for every newly authored preset.
        public int schemaVersion = 1;
        [InspectorName("プリセット名")]
        public string presetName = "";
        [InspectorName("Root トラッキングポリシー")]
        public PoseTrackingPolicyPresetData rootTrackingPolicy = new();
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
