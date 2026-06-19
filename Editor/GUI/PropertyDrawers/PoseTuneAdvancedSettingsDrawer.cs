using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseTuneAdvancedSettings))]
    public sealed class PoseTuneAdvancedSettingsDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("allowFullBodyTracking", "FBT を許可"),
            new("actionWeightControlMode", "Action Weight 制御"),
            new("lockDesktopLowerBodyTracking", "Desktop 下半身固定"),
            new("keepGeneratedObjectsInBuild", "生成オブジェクトを Build に残す")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
