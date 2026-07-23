using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseClipPresetData))]
    public sealed class PoseClipPresetDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("poseStableGuid", "Pose Stable GUID"),
            new("displayName", "表示名"),
            new("clip", "クリップ"),
            new("sourceMotion", "元 Motion"),
            new("compatibilityProfile", "互換プロファイル"),
            new("adjustmentClip", "調整クリップ"),
            new("adjustmentApplyMode", "調整適用モード"),
            new("icon", "アイコン"),
            new("rootYawOffsetDegrees", "Root Yaw オフセット"),
            new("humanoidOrientationOffsetYDegrees", "Humanoid Orientation Offset Y"),
            new("recenterRootXZToHead", "Root XZ を first key 基準で再中心化"),
            new("menuOrder", "メニュー順"),
            new("isInitial", "初期ポーズ"),
            new("loop", "ループ"),
            new("explicitMenuValue", "明示メニュー値"),
            new("sourceSyncedParameterValue", "移行元同期値"),
            new("rootOffset", "Root オフセット"),
            new("cameraOffset", "カメラオフセット"),
            new("priority", "優先度"),
            new("blendMode", "ブレンドモード"),
            new("emitTrackingControl", "トラッキング制御を生成"),
            new("trackingPolicy", "トラッキングポリシー"),
            new("suppressIconGeneration", "アイコン生成を抑止"),
            new("motionTime", "モーション時間"),
            new("poseSpace", "ポーズ空間"),
            new("clipConditions", "クリップ条件")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
