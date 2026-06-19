using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseCondition))]
    [CanEditMultipleObjects]
    public sealed class PoseConditionEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("composition", "条件の合成"),
                new PoseTuneFieldLabel("conditions", "条件"));
        }
    }
}
