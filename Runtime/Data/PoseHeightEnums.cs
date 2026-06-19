using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum HeightApplyMode
    {
        [InspectorName("Root または Hips の Y オフセット")]
        RootOrHipsYOffset = 0,
        [InspectorName("無効")]
        Disabled = 1,
        [InspectorName("Humanoid Level オフセット")]
        HumanoidLevelOffset = 2
    }

    public enum HeightBlendProfile
    {
        [InspectorName("標準")]
        Standard,
        [InspectorName("KawaiiPosing")]
        KawaiiPosing
    }

    public enum HeightAutoCorrectionMode
    {
        [InspectorName("無効")]
        Disabled,
        [InspectorName("Runtime ScaleFactor")]
        RuntimeScaleFactor,
        [InspectorName("Runtime EyeHeight")]
        RuntimeEyeHeightMeters
    }
}
