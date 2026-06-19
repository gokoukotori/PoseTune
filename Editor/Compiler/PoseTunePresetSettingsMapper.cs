using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTunePresetSettingsMapper
    {
        public static void CaptureMenu(PoseMenuPresetData target, PoseMenu source)
        {
            target.rootMenuName = source.rootMenuName;
            target.autoSplitMenu = source.autoSplitMenu;
            target.installMode = source.installMode;
            target.generateIcons = source.generateIcons;
            target.useSubMenusPerGroup = source.useSubMenusPerGroup;
            target.lyingMenuLayout = source.lyingMenuLayout;
        }

        public static void CaptureHeight(PoseHeightPresetData target, PoseHeightAdjust source)
        {
            target.enabled = PoseTuneAuthoringInclusion.Includes(source);
            target.parameterName = source.parameterName;
            target.min = source.min;
            target.max = source.max;
            target.applyMode = source.applyMode;
            target.blendProfile = source.blendProfile;
            target.lowOffset = source.lowOffset;
            target.midOffset = source.midOffset;
            target.highOffset = source.highOffset;
            target.autoCorrectionMode = source.autoCorrectionMode;
            target.referenceEyeHeightMeters = source.referenceEyeHeightMeters;
            target.maxAutoOffset = source.maxAutoOffset;
            target.generateRadialMenu = source.generateRadialMenu;
            target.saved = source.saved;
            target.synced = source.synced;
        }

        public static void ApplyMenu(PoseMenu target, PoseMenuPresetData source)
        {
            target.rootMenuName = source.rootMenuName;
            target.autoSplitMenu = source.autoSplitMenu;
            target.installMode = source.installMode;
            target.generateIcons = source.generateIcons;
            target.useSubMenusPerGroup = source.useSubMenusPerGroup;
            target.lyingMenuLayout = source.lyingMenuLayout;
        }

        public static void ApplyHeight(PoseHeightAdjust target, PoseHeightPresetData source)
        {
            target.includeInBuild = source.enabled;
            target.parameterName = source.parameterName;
            target.min = source.min;
            target.max = source.max;
            target.applyMode = source.applyMode;
            target.blendProfile = source.blendProfile;
            target.lowOffset = source.lowOffset;
            target.midOffset = source.midOffset;
            target.highOffset = source.highOffset;
            target.autoCorrectionMode = source.autoCorrectionMode;
            target.referenceEyeHeightMeters = source.referenceEyeHeightMeters;
            target.maxAutoOffset = source.maxAutoOffset;
            target.generateRadialMenu = source.generateRadialMenu;
            target.saved = source.saved;
            target.synced = source.synced;
        }
    }
}
