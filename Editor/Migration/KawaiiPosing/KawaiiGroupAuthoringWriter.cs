using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiGroupAuthoringWriter
    {
        public static void CreateLayer(
            Transform parent,
            KawaiiPosingSystemDto dto,
            KawaiiLayerDto layer,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            string undoName,
            IKawaiiMigrationAssetStore assetStore = null)
        {
            var groupObject = KawaiiAuthoringObjectUtility.CreateChild(
                parent,
                KawaiiAuthoringObjectUtility.SafeName(layer.MenuName, "Kawaii Group " + layer.Index),
                undoName);
            var group = Undo.AddComponent<PoseGroup>(groupObject);
            group.kind = KawaiiPosingMapper.MapGroupKind(layer);
            group.displayName = KawaiiAuthoringObjectUtility.SafeName(layer.MenuName, PoseTuneTemplateFactory.DefaultDisplayName(group.kind));
            group.parameterName = options.preserveSourceParameterNames ? layer.ParameterName : "";
            group.menuOrder = layer.Index * 10;
            group.icon = dto != null && dto.IsIconDisabled ? null : layer.Icon;
            group.activationMode = PoseGroupActivationMode.ManualAndAuto;
            group.autoPoseSelectionMode = AutoPoseSelectionMode.SelectedPosePerGroup;
            group.autoContextProfile = AutoContextProfile.KawaiiHeadHeightApproximation;
            group.emitTrackingControl = options.addTrackingPolicy;
            report.Created(groupObject, "Group");

            AddGroupTrackingPolicy(groupObject, group.kind, options);

            // KawaiiMigrationPlanner already reported the skipped disabled poses (PT-K004).
            foreach (var animation in KawaiiPosingMapper.ImportableAnimations(layer, options))
            {
                KawaiiPoseAuthoringWriter.CreatePose(
                    group,
                    animation,
                    options,
                    report,
                    dto != null && dto.IsIconDisabled,
                    undoName,
                    assetStore);
            }
        }

        internal static void AddGroupTrackingPolicy(
            GameObject groupObject,
            PoseGroupKind kind,
            KawaiiMigrationOptions options)
        {
            if (!options.addTrackingPolicy)
            {
                return;
            }

            var policy = Undo.AddComponent<PoseTrackingPolicy>(groupObject);
            policy.tracking = KawaiiPosingMapper.DefaultTracking(kind);
            if (options.disableWhenFullBodyTracking)
            {
                policy.useFullBodyTrackingOverride = true;
                policy.fullBodyTracking = TrackingPolicyData.ResetToTracking();
            }
        }
    }
}
