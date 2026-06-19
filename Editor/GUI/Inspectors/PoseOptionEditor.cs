using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseOption))]
    [CanEditMultipleObjects]
    public sealed class PoseOptionEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(new PoseTuneFieldLabel("options", "オプション"));
        }
    }
}
