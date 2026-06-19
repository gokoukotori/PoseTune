using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiFootHeightCompatibilityConverter
    {
        // TODO: Use this as the shared entry point if migration and presets need separate strict-profile setup.
        public static void ApplyStrictProfile(PoseHeightAdjust height)
        {
            if (height == null)
            {
                return;
            }

            height.parameterName = "FootHeight";
            height.applyMode = HeightApplyMode.HumanoidLevelOffset;
            height.blendProfile = HeightBlendProfile.KawaiiPosing;
            height.lowOffset = 2f;
            height.midOffset = 0f;
            height.highOffset = -2f;
        }
    }
}
