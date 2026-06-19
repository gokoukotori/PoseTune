using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(TrackingPolicyData))]
    public sealed class TrackingPolicyDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("head", "頭"),
            new("leftHand", "左手"),
            new("rightHand", "右手"),
            new("hip", "腰"),
            new("leftFoot", "左足"),
            new("rightFoot", "右足"),
            new("leftFingers", "左指"),
            new("rightFingers", "右指"),
            new("eyes", "目"),
            new("mouth", "口")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
