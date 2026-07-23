using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseClip))]
    [CanEditMultipleObjects]
    public sealed class PoseClipEditor : PoseTuneLocalizedEditor
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("includeInBuild", "ビルドに含める"),
            new("displayName", "表示名"),
            new("clip", "クリップ"),
            new("sourceMotion", "元 Motion"),
            new("compatibilityProfile", "互換プロファイル"),
            new("adjustmentClip", "調整クリップ"),
            new("adjustmentApplyMode", "調整適用モード"),
            new("customIcon", "カスタムアイコン"),
            new("rootOffset", "Root オフセット"),
            new("rootYawOffsetDegrees", "Root Yaw オフセット"),
            new("humanoidOrientationOffsetYDegrees", "Humanoid Orientation Offset Y"),
            new("recenterRootXZToHead", "Root XZ を first key 基準で再中心化"),
            new("cameraOffset", "カメラオフセット", "サムネイル生成時のカメラ位置補正です。VRChat 実行時の視点位置は変更しません。"),
            new("isInitial", "初期ポーズ"),
            new("loop", "ループ"),
            new("explicitMenuValue", "明示メニュー値"),
            new("sourceSyncedParameterValue", "移行元同期値"),
            new("menuOrder", "メニュー順"),
            new("priority", "優先度", "自動ポーズ選択と Animator transition の生成順に使います。"),
            new("blendMode", "ブレンドモード", "Animator layer の Override / Additive 分割に使います。"),
            new("motionTime", "モーション時間"),
            new("poseSpace", "ポーズ空間"),
            new("emitTrackingControl", "トラッキング制御を生成"),
            new("suppressIconGeneration", "アイコン生成を抑止"),
            new("clipConditions", "クリップ条件")
        };

        public override void OnInspectorGUI()
        {
            DrawFields(FieldLabels);
        }
    }
}
