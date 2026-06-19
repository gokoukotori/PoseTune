using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseHeightPresetData))]
    public sealed class PoseHeightPresetDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("enabled", "有効"),
            new("parameterName", "パラメータ名"),
            new("min", "最小値"),
            new("max", "最大値"),
            new("applyMode", "適用モード"),
            new("blendProfile", "高さ Blend プロファイル"),
            new("lowOffset", "低値オフセット"),
            new("midOffset", "中央オフセット"),
            new("highOffset", "高値オフセット"),
            new("autoCorrectionMode", "自動補正"),
            new("referenceEyeHeightMeters", "基準 EyeHeight(m)"),
            new("maxAutoOffset", "最大自動オフセット"),
            new("generateRadialMenu", "Radial Puppet メニューを生成"),
            new("saved", "保存"),
            new("synced", "同期")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
