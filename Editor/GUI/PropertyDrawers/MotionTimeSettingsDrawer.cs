using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(MotionTimeSettings))]
    public sealed class MotionTimeSettingsDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("mode", "モード"),
            new("parameterName", "パラメータ名"),
            new("generateRadialMenu", "Radial Puppet メニューを生成", "Expression Menu から操作できる Float parameter の Radial Puppet を生成します。")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
