using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTrackingPolicy))]
    [CanEditMultipleObjects]
    public sealed class PoseTrackingPolicyEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("tracking", "トラッキング"),
                new PoseTuneFieldLabel("useFullBodyTrackingOverride", "FBT 用トラッキングを上書き"),
                new PoseTuneFieldLabel("fullBodyTracking", "FBT 用トラッキング"),
                new PoseTuneFieldLabel("generateResetOnExit", "終了時にリセットを生成", "Pose 終了時に、この policy が明示変更した部位だけを Tracking へ戻す reset request を生成します。"));
        }
    }
}
