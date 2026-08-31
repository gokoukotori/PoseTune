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
            var groupObject = new GameObject(SafeName(layer.MenuName, "Kawaii Group " + layer.Index));
            Undo.RegisterCreatedObjectUndo(groupObject, undoName);
            Undo.SetTransformParent(groupObject.transform, parent, undoName);
            var group = Undo.AddComponent<PoseGroup>(groupObject);
            group.kind = KawaiiPosingMapper.MapGroupKind(layer);
            group.displayName = SafeName(layer.MenuName, PoseTuneTemplateFactory.DefaultDisplayName(group.kind));
            group.parameterName = options.preserveSourceParameterNames ? layer.ParameterName : "";
            group.menuOrder = layer.Index * 10;
            group.icon = dto != null && dto.IsIconDisabled ? null : layer.Icon;
            group.activationMode = PoseGroupActivationMode.ManualAndAuto;
            group.autoPoseSelectionMode = AutoPoseSelectionMode.SelectedPosePerGroup;
            group.autoContextProfile = AutoContextProfile.KawaiiHeadHeightApproximation;
            group.emitTrackingControl = options.addTrackingPolicy;
            report.Created(groupObject, "Group");

            if (options.addTrackingPolicy)
            {
                var policy = Undo.AddComponent<PoseTrackingPolicy>(groupObject);
                policy.tracking = KawaiiPosingMapper.DefaultTracking(group.kind);
                if (options.disableWhenFullBodyTracking)
                {
                    policy.useFullBodyTrackingOverride = true;
                    policy.fullBodyTracking = TrackingPolicyData.ResetToTracking();
                }
            }

            foreach (var animation in KawaiiPosingMapper.ImportableAnimations(layer, options, report))
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

        private static string SafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
