using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseTuneOptions))]
    public sealed class PoseTuneOptionsDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("lockHead", "頭をロック"),
            new("lockHands", "手をロック"),
            new("lockFeet", "足をロック"),
            new("locomotionLock", "移動ロック")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
