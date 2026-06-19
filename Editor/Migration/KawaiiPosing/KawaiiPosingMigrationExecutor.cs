using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiPosingMigrationExecutor
    {
        private const string UndoName = "KawaiiPosing から PoseTune へ移行";

        public KawaiiMigrationReport Execute(
            GameObject avatarRoot,
            IEnumerable<MonoBehaviour> sourceComponents,
            KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            var report = new KawaiiMigrationReport();
            var sources = (sourceComponents ?? Enumerable.Empty<MonoBehaviour>())
                .Where(component => component != null)
                .ToList();
            report.SourceSystemCount = sources.Count;

            if (avatarRoot == null || ResolveAvatar(avatarRoot) == null)
            {
                report.Error(PoseTuneDiagnostics.KawaiiMigrationSourceMissing.Code, "VRCAvatarDescriptor を持つ avatar が見つかりません。", avatarRoot);
                return report;
            }

            if (sources.Count == 0)
            {
                report.Error(PoseTuneDiagnostics.KawaiiMigrationSourceMissing.Code, "KawaiiPosing / PosingSystem が見つかりません。", avatarRoot);
                return report;
            }

            var dtos = sources.Select(PosingSystemSerializedReader.Read).ToList();
            foreach (var dto in dtos)
            {
                foreach (var warning in dto.Warnings)
                {
                    report.Warning(warning.Code, warning.Message, warning.Context);
                }

                if (dto.ThumbnailPackObject != null)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiThumbnailPackNotMigrated.Code,
                        "thumbnailPackObject は未移行です。custom icon / previewImage のみ PoseTune icon に移行します。",
                        dto.ThumbnailPackObject);
                }
            }

            ReportSourcePolicyLimitations(dtos, options, report);
            CountPlan(dtos, options, report);
            ValidateOptions(options, report, avatarRoot);
            report.Info(PoseTuneDiagnostics.KawaiiMigrationOptionsInfo.Code, "Options: " + KawaiiMigrationOptionSupport.Summary(options), avatarRoot);
            if (options.dryRunOnly || report.HasErrors)
            {
                report.Info(PoseTuneDiagnostics.KawaiiMigrationSummaryInfo.Code, $"Dry-run: groups={report.CreatedGroupCount}, poses={report.CreatedPoseCount}", avatarRoot);
                return report;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                var root = KawaiiRootAuthoringWriter.PrepareRoot(
                    avatarRoot,
                    options,
                    dtos,
                    report,
                    UndoName);
                var groupsRoot = KawaiiRootAuthoringWriter.EnsureGroupsRoot(root, report, UndoName);

                foreach (var dto in dtos)
                {
                    foreach (var layer in dto.Layers.OrderBy(layer => layer.Index))
                    {
                        KawaiiGroupAuthoringWriter.CreateLayer(
                            groupsRoot.transform,
                            dto,
                            layer,
                            options,
                            report,
                            UndoName);
                    }

                    KawaiiOverrideAuthoringWriter.ImportOverrides(
                        groupsRoot.transform,
                        dto,
                        options,
                        report,
                        UndoName);
                }

                foreach (var source in sources)
                {
                    PostprocessSource(source, options);
                }

                PoseTuneStableGuidRepair.Repair(root);
                EditorUtility.SetDirty(root);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            report.Info(PoseTuneDiagnostics.KawaiiMigrationSummaryInfo.Code, $"Kawaii migration completed: groups={report.CreatedGroupCount}, poses={report.CreatedPoseCount}", avatarRoot);
            return report;
        }

        private static void ReportSourcePolicyLimitations(
            IEnumerable<KawaiiPosingSystemDto> dtos,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            foreach (var dto in dtos ?? Enumerable.Empty<KawaiiPosingSystemDto>())
            {
                if (options.addTrackingPolicy && !dto.MergeTrackingControl)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiSourceMergeTrackingControlDisabled.Code,
                        "source mergeTrackingControl が false のため、PoseTune tracking policy import を無効化しました。",
                        dto.SourceComponent);
                }

                if (dto.AutoImportAvatarAnimations)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiSourceAutoImportAvatarAnimationsUnsupported.Code,
                        "source autoImportAvatarAnimations は PoseTune migration では自動再現されません。必要な locomotion override は手動確認してください。",
                        dto.SourceComponent);
                }
            }
        }

        private static void CountPlan(
            IEnumerable<KawaiiPosingSystemDto> dtos,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            foreach (var dto in dtos)
            {
                foreach (var layer in dto.Layers)
                {
                    report.CreatedGroupCount++;
                    foreach (var animation in layer.Animations)
                    {
                        if (!animation.Enabled && !options.preserveDisabledPosesAsDisabled)
                        {
                            report.SkippedPoseCount++;
                            continue;
                        }

                        var expanded = KawaiiBlendTreeCompatibilityConverter.ExpandAnimation(animation, options);
                        report.CreatedPoseCount += expanded.Count;
                        if (animation.BlendTree != null)
                        {
                            report.BlendTreePoseCount++;
                        }
                    }
                }
            }
        }

        private static void ValidateOptions(
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            UnityEngine.Object context)
        {
        }

        private static void PostprocessSource(MonoBehaviour source, KawaiiMigrationOptions options)
        {
            if (source == null)
            {
                return;
            }

            if (options.tagSourceAsEditorOnly)
            {
                Undo.RecordObject(source.gameObject, UndoName);
                source.gameObject.tag = "EditorOnly";
            }

            if (options.disableSourceKawaiiObjectAfterMigration)
            {
                Undo.RecordObject(source.gameObject, UndoName);
                source.gameObject.SetActive(false);
            }
        }

        private static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            return selected != null
                ? selected.GetComponent<VRCAvatarDescriptor>()
                  ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                  ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true)
                : null;
        }

    }
}
