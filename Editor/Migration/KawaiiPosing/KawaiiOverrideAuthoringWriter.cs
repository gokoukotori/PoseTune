using System;
using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiOverrideAuthoringWriter
    {
        public static void ImportOverrides(
            Transform parent,
            KawaiiPosingSystemDto dto,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            string undoName,
            IKawaiiMigrationAssetStore assetStore = null)
        {
            if (options.overrideImportMode == KawaiiOverrideImportMode.ReportOnly)
            {
                foreach (var item in dto.Overrides)
                {
                    report.Warning(PoseTuneDiagnostics.KawaiiOverrideUnsupported.Code, "overrideDefine は report only です: " + item.StateType, dto.SourceComponent);
                }

                return;
            }

            foreach (var item in dto.Overrides)
            {
                if (!ShouldImport(item, options))
                {
                    if (item.Enabled &&
                        KawaiiPosingMapper.MapOverrideKind(item.StateType) == PoseGroupKind.Custom &&
                        options.overrideImportMode == KawaiiOverrideImportMode.ImportSupportedOnly)
                    {
                        report.Warning(
                            PoseTuneDiagnostics.KawaiiOverrideUnsupported.Code,
                            "unsupported override state: " + item.StateType,
                            dto.SourceComponent);
                    }

                    continue;
                }

                var group = KawaiiAuthoringObjectUtility.CreateChild(
                    parent,
                    OverrideName(item),
                    undoName);
                var poseGroup = Undo.AddComponent<PoseGroup>(group);
                poseGroup.kind = ImportedKind(item, options);
                poseGroup.displayName = OverrideName(item);
                poseGroup.menuOrder = 1000 + item.Index;
                poseGroup.includeInBuild =
                    options.overrideImportMode != KawaiiOverrideImportMode.ImportAllAsCustomDisabled;
                poseGroup.activationMode = PoseGroupActivationMode.ManualAndAuto;
                poseGroup.autoPoseSelectionMode = AutoPoseSelectionMode.InitialPoseOnly;
                poseGroup.autoContextProfile = AutoContextProfile.KawaiiHeadHeightApproximation;
                poseGroup.emitTrackingControl = options.addTrackingPolicy;
                KawaiiGroupAuthoringWriter.AddGroupTrackingPolicy(group, poseGroup.kind, options);

                var animations = ExpandAnimations(item, options);
                for (var index = 0; index < animations.Count; index++)
                {
                    if (index == 0)
                    {
                        var pose = Undo.AddComponent<PoseClip>(group);
                        KawaiiPoseAuthoringWriter.ConfigurePose(
                            pose,
                            poseGroup,
                            animations[index],
                            options,
                            report,
                            dto.IsIconDisabled,
                            assetStore);
                        continue;
                    }

                    KawaiiPoseAuthoringWriter.CreatePose(
                        poseGroup,
                        animations[index],
                        options,
                        report,
                        dto.IsIconDisabled,
                        undoName,
                        assetStore);
                }

                report.Created(group, "Override");
            }
        }

        internal static bool ShouldImport(KawaiiOverrideDto item, KawaiiMigrationOptions options)
        {
            if (item == null || !item.Enabled || options == null ||
                options.overrideImportMode == KawaiiOverrideImportMode.ReportOnly)
            {
                return false;
            }

            return options.overrideImportMode != KawaiiOverrideImportMode.ImportSupportedOnly ||
                   KawaiiPosingMapper.MapOverrideKind(item.StateType) != PoseGroupKind.Custom;
        }

        internal static PoseGroupKind ImportedKind(KawaiiOverrideDto item, KawaiiMigrationOptions options)
        {
            return options.overrideImportMode == KawaiiOverrideImportMode.ImportAllAsCustomDisabled
                ? PoseGroupKind.Custom
                : KawaiiPosingMapper.MapOverrideKind(item.StateType);
        }

        internal static IReadOnlyList<KawaiiAnimationDto> ExpandAnimations(
            KawaiiOverrideDto item,
            KawaiiMigrationOptions options)
        {
            if (item == null)
            {
                return Array.Empty<KawaiiAnimationDto>();
            }

            var animation = new KawaiiAnimationDto { DisplayName = OverrideName(item) };
            item.CopyMotionEntryTo(animation);
            animation.Enabled = item.Enabled &&
                                options.overrideImportMode != KawaiiOverrideImportMode.ImportAllAsCustomDisabled;
            return KawaiiBlendTreeCompatibilityConverter.ExpandAnimation(animation, options);
        }

        private static string OverrideName(KawaiiOverrideDto item)
        {
            return "Override " + KawaiiAuthoringObjectUtility.SafeName(item.StateType, "Custom");
        }
    }
}
