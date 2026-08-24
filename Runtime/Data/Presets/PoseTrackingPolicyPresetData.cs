using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseTrackingPolicyPresetData
    {
        [InspectorName("有効なコンポーネントあり")]
        [Tooltip("ON の場合は有効な PoseTrackingPolicy を表します。無効な component は存在しないものとして扱います。")]
        public bool present;
        [InspectorName("トラッキング")]
        public TrackingPolicyData tracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("FBT 用トラッキングを上書き")]
        public bool useFullBodyTrackingOverride;
        [InspectorName("FBT 用トラッキング")]
        public TrackingPolicyData fullBodyTracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("終了時にリセットを生成")]
        public bool generateResetOnExit = true;
    }
}
