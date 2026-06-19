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
                new PoseTuneFieldLabel("generateResetOnExit", "終了時にリセットを生成", "Pose 終了時に VRChat tracking を Tracking に戻す cleanup behavior を生成します。"));
        }
    }
}
