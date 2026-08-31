using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneCompilerRules
    {
        public static bool AllowsManualControl(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return root == null ||
                   group == null ||
                   !root.enableAutoContextSwitch ||
                   group.ActivationMode != PoseGroupActivationMode.Auto;
        }

        public static bool RequiresPoseSelectionParameter(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return AllowsManualControl(root, group) ||
                   root != null &&
                   group != null &&
                   root.enableAutoContextSwitch &&
                   group.ActivationMode == PoseGroupActivationMode.Auto &&
                   group.AutoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup;
        }

        public static bool UsesSelectedPoseAutoSelection(PoseTuneRoot root, PoseGroupDefinition group)
        {
            return root != null &&
                   group != null &&
                   root.enableAutoContextSwitch &&
                   group.ActivationMode != PoseGroupActivationMode.Manual &&
                   group.AutoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup;
        }

        public static bool ControlsActionPlayable(PoseTuneRoot root)
        {
            return root != null &&
                   root.targetLayer == PoseTuneTargetLayer.Action &&
                   root.advancedSettings.actionWeightControlMode != ActionWeightControlMode.Disabled;
        }
    }
}
