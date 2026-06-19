using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseTunePreviewSettings))]
    public sealed class PoseTunePreviewSettingsDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("thumbnailSize", "サムネイルサイズ"),
            new("backgroundColor", "背景色")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
