using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(StableComponentGuid))]
    public sealed class StableComponentGuidDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("value", "値")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
