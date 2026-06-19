using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseAdjustmentEntry
    {
        [InspectorName("Pose Stable GUID")]
        public string poseStableGuid = "";
        [InspectorName("調整クリップ")]
        public AnimationClip adjustmentClip;
        [InspectorName("Root オフセット")]
        public Vector3 rootOffset;
        [InspectorName("カメラオフセット")]
        public Vector3 cameraOffset;
    }
}
