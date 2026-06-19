using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseSpacePolicy))]
    public sealed class PoseSpacePolicyDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("enabled", "有効"),
            new("scope", "適用スコープ"),
            new("enterPoseSpace", "ポーズ空間に入る"),
            new("fixedDelay", "固定ディレイ"),
            new("delayTime", "ディレイ時間")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
