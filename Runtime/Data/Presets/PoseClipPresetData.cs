using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseClipPresetData
    {
        [InspectorName("Pose Stable GUID")]
        public string poseStableGuid = "";
        [InspectorName("表示名")]
        public string displayName = "";
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
        [InspectorName("アイコン")]
        public Texture2D icon;
        [InspectorName("Root Yaw オフセット")]
        public float rootYawOffsetDegrees;
        [InspectorName("Humanoid Orientation Offset Y")]
        public float humanoidOrientationOffsetYDegrees;
        [InspectorName("Root XZ を first key 基準で再中心化")]
        public bool recenterRootXZToHead;
        [InspectorName("メニュー順")]
        public int menuOrder;
        [InspectorName("初期ポーズ")]
        public bool isInitial;
        [InspectorName("ループ")]
        public bool loop = true;
        [InspectorName("明示メニュー値")]
        public int explicitMenuValue;
        [InspectorName("移行元同期値")]
        public int sourceSyncedParameterValue;
        [InspectorName("Root オフセット")]
        public Vector3 rootOffset;
        [InspectorName("カメラオフセット")]
        public Vector3 cameraOffset;
        [InspectorName("優先度")]
        public PoseClipPriority priority = PoseClipPriority.Normal;
        [InspectorName("ブレンドモード")]
        public PoseClipBlendMode blendMode = PoseClipBlendMode.Override;
        [InspectorName("トラッキング")]
        public TrackingPolicyData tracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("トラッキング制御を生成")]
        public bool emitTrackingControl = true;
        [InspectorName("アイコン生成を抑止")]
        public bool suppressIconGeneration;
        [InspectorName("Motion Time")]
        public MotionTimeSettings motionTime = new();
        [InspectorName("ポーズ空間")]
        public PoseSpacePolicy poseSpace = new();
        [InspectorName("クリップ条件")]
        public List<ParameterConditionData> clipConditions = new();
    }
}
