using System.Linq;
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

        /// <summary>Pose motions are built with the generated height offset applied.</summary>
        public static bool AppliesHeightOffset(PoseGraph graph)
        {
            return graph.RootComponent.enableHeightAdjust &&
                   PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                   graph.HeightAdjust.applyMode != HeightApplyMode.Disabled;
        }

        /// <summary>Kinds whose poses drive the lower body (sitting and lying).</summary>
        public static bool IsLowerBodyKind(PoseGroupKind kind)
        {
            return kind == PoseGroupKind.Chair ||
                   kind == PoseGroupKind.Floor ||
                   kind == PoseGroupKind.Prone ||
                   kind == PoseGroupKind.Supine;
        }

        public static bool IsLyingKind(PoseGroupKind kind)
        {
            return kind == PoseGroupKind.Prone || kind == PoseGroupKind.Supine;
        }

        public static bool IsLyingGroup(PoseGroupDefinition group)
        {
            return IsLyingKind(group.Kind);
        }

        /// <summary>Auto context switching tells prone from supine through the generated SupineFlag.</summary>
        public static bool NeedsSupineFlag(PoseGraph graph)
        {
            return graph.RootComponent.enableAutoContextSwitch &&
                   PoseGraphBuildFilter.BuildableGroups(graph).Any(IsLyingGroup);
        }
    }
}
