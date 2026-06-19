using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseTuneGoroneSystemExCompatibility))]
    [CanEditMultipleObjects]
    public sealed class PoseTuneGoroneSystemExCompatibilityEditor : PoseTuneLocalizedEditor
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("guardMode", "ガードモード"),
            new("requireGoroneSystemEx", "Gorone System EX を必須にする"),
            new("overridePoseTuneLayerPriority", "PoseTune レイヤー優先度を上書き"),
            new("poseTuneLayerPriority", "PoseTune レイヤー優先度")
        };

        public override void OnInspectorGUI()
        {
            DrawFields(FieldLabels);
        }
    }
}
