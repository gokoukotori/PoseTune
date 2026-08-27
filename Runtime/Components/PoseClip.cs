using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Clip")]
    public sealed class PoseClip : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("ビルドに含める")]
        public bool includeInBuild = true;
        [InspectorName("表示名")]
        public string displayName = "ポーズ";
        [InspectorName("クリップ")]
        public AnimationClip clip;
        [InspectorName("元 Motion")]
        public Motion sourceMotion;
        [InspectorName("互換プロファイル")]
        public PoseSourceCompatibilityProfile compatibilityProfile;
        [InspectorName("調整クリップ")]
        public AnimationClip adjustmentClip;
        [InspectorName("調整適用モード")]
        public PoseAdjustmentApplyMode adjustmentApplyMode = PoseAdjustmentApplyMode.ReplaceCurves;
        [InspectorName("カスタムアイコン")]
        public Texture2D customIcon;
        [InspectorName("Root オフセット")]
        public Vector3 rootOffset;
        [InspectorName("Root Yaw オフセット")]
        public float rootYawOffsetDegrees;
        [InspectorName("Humanoid Orientation Offset Y")]
        public float humanoidOrientationOffsetYDegrees;
        [InspectorName("Root XZ を first key 基準で再中心化")]
        public bool recenterRootXZToHead;
        [InspectorName("カメラオフセット")]
        [Tooltip("サムネイル生成時のカメラ位置補正です。VRChat 実行時の視点位置は変更しません。")]
        public Vector3 cameraOffset;

        [InspectorName("初期ポーズ")]
        public bool isInitial;
        [InspectorName("ループ")]
        public bool loop = true;
        [InspectorName("明示メニュー値")]
        public int explicitMenuValue;
        [InspectorName("移行元同期値")]
        public int sourceSyncedParameterValue;
        [InspectorName("メニュー順")]
        public int menuOrder;

        [InspectorName("優先度")]
        [Tooltip("自動ポーズ選択と Animator transition の生成順に使います。")]
        public PoseClipPriority priority = PoseClipPriority.Normal;
        [InspectorName("ブレンドモード")]
        [Tooltip("Animator layer の Override / Additive 分割に使います。")]
        public PoseClipBlendMode blendMode = PoseClipBlendMode.Override;

        [InspectorName("Motion Time")]
        public MotionTimeSettings motionTime = new();
        [InspectorName("ポーズ空間")]
        public PoseSpacePolicy poseSpace = new();
        [InspectorName("アイコン生成を抑止")]
        public bool suppressIconGeneration;
        [InspectorName("クリップ条件")]
        public List<ParameterConditionData> clipConditions = new();
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                var sourceName = clip != null ? clip.name : sourceMotion != null ? sourceMotion.name : gameObject.name;
                displayName = ObjectNamesFallback.Nicify(sourceName);
            }
        }
    }
}
