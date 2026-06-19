using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTuneGeneratedMarker))]
    [CanEditMultipleObjects]
    public sealed class PoseTuneGeneratedMarkerEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("rootGuid", "Root GUID"),
                new PoseTuneFieldLabel("generatedVersion", "生成バージョン"),
                new PoseTuneFieldLabel("graphHash", "グラフハッシュ"),
                new PoseTuneFieldLabel("generatedAt", "生成日時"));
        }
    }
}
