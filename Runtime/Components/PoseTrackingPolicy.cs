using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Tracking Policy")]
    public sealed class PoseTrackingPolicy : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("トラッキング")]
        public TrackingPolicyData tracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("FBT 用トラッキングを上書き")]
        public bool useFullBodyTrackingOverride;
        [InspectorName("FBT 用トラッキング")]
        public TrackingPolicyData fullBodyTracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("終了時にリセットを生成")]
        [Tooltip("Pose 終了時に VRChat tracking を Tracking に戻す cleanup behavior を生成します。")]
        public bool generateResetOnExit = true;
    }
}
