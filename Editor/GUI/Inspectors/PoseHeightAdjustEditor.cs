using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseHeightAdjust))]
    [CanEditMultipleObjects]
    public sealed class PoseHeightAdjustEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("includeInBuild", "ビルドに含める"),
                new PoseTuneFieldLabel("parameterName", "パラメータ名"),
                new PoseTuneFieldLabel("min", "最小値"),
                new PoseTuneFieldLabel("max", "最大値"),
                new PoseTuneFieldLabel("applyMode", "適用モード"),
                new PoseTuneFieldLabel("blendProfile", "高さ Blend プロファイル"),
                new PoseTuneFieldLabel("lowOffset", "低値オフセット"),
                new PoseTuneFieldLabel("midOffset", "中央オフセット"),
                new PoseTuneFieldLabel("highOffset", "高値オフセット"),
                new PoseTuneFieldLabel("autoCorrectionMode", "自動補正"),
                new PoseTuneFieldLabel("referenceEyeHeightMeters", "基準 EyeHeight(m)"),
                new PoseTuneFieldLabel("maxAutoOffset", "最大自動オフセット"),
                new PoseTuneFieldLabel("generateRadialMenu", "Radial Puppet メニューを生成", "高さの Radial Puppet メニュー項目を生成します。"),
                new PoseTuneFieldLabel("saved", "保存"),
                new PoseTuneFieldLabel("synced", "同期"));
        }
    }
}
