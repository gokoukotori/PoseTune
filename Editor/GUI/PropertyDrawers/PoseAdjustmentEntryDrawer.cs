using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseAdjustmentEntry))]
    public sealed class PoseAdjustmentEntryDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("poseStableGuid", "Pose Stable GUID"),
            new("adjustmentClip", "調整クリップ"),
            new("rootOffset", "Root オフセット"),
            new("cameraOffset", "カメラオフセット")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
