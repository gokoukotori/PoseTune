using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Tracking Policy")]
    [DisallowMultipleComponent]
    public sealed class PoseTrackingPolicy : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("トラッキング")]
        public TrackingPolicyData tracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("FBT 用トラッキングを上書き")]
        public bool useFullBodyTrackingOverride;
        [InspectorName("FBT 用トラッキング")]
        public TrackingPolicyData fullBodyTracking = TrackingPolicyData.DefaultForPose();
        [InspectorName("終了時にリセットを生成")]
        [Tooltip("Group 内の Pose 終了時に、この policy が明示変更した部位だけを Tracking へ戻す reset request を生成します。")]
        public bool generateResetOnExit = true;
    }
}
