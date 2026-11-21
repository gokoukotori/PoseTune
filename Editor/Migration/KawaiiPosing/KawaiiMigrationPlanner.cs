using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiMigrationPlan
    {
        internal GameObject AvatarRoot;
        internal VRCAvatarDescriptor Avatar;
        internal List<MonoBehaviour> Sources = new();
        internal List<KawaiiPosingSystemDto> Dtos = new();
        internal KawaiiMigrationOptions Options;
        internal KawaiiMigrationReport Report;

        internal bool IsValid => Report != null && !Report.HasErrors;
    }

    internal static class KawaiiMigrationPlanner
    {
        internal const string TargetMismatchCode = "PT-K005";
        private const string SourceScopeCode = "PT-K006";
        private const string SourceRootMutationCode = "PT-K007";
        private const string SharedSourceConfirmationCode = "PT-K008";
        private const string AssetPreflightCode = "PT-K009";

        internal static KawaiiMigrationPlan CreatePlan(
            GameObject avatarRoot,
            IEnumerable<MonoBehaviour> sourceComponents,
            KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            var report = new KawaiiMigrationReport();
            var plan = new KawaiiMigrationPlan
            {
                AvatarRoot = avatarRoot,
                Options = options,
                Report = report
            };
            plan.Sources = (sourceComponents ?? Enumerable.Empty<MonoBehaviour>())
                .Where(component => component != null)
                .Distinct()
                .ToList();
            report.SourceSystemCount = plan.Sources.Count;

            plan.Avatar = PoseTuneHierarchyUtility.ResolveAvatar(avatarRoot);
            if (avatarRoot == null || plan.Avatar == null)
            {
                report.Error(
                    PoseTuneDiagnostics.KawaiiMigrationSourceMissing.Code,
                    "VRCAvatarDescriptor を持つ avatar が見つかりません。",
                    avatarRoot);
                return plan;
            }

            plan.AvatarRoot = plan.Avatar.gameObject;
            if (plan.Sources.Count == 0)
            {
                report.Error(
                    PoseTuneDiagnostics.KawaiiMigrationSourceMissing.Code,
                    "KawaiiPosing / PosingSystem が見つかりません。",
                    plan.AvatarRoot);
                return plan;
            }

            plan.Dtos = plan.Sources.Select(PosingSystemSerializedReader.Read).ToList();
            foreach (var dto in plan.Dtos)
            {
                foreach (var warning in dto.Warnings)
                {
                    if (warning.Code == PoseTuneDiagnostics.KawaiiSerializedReadWarning.Code)
                    {
                        report.Error(warning.Code, warning.Message, warning.Context);
                    }
                    else
                    {
                        report.Warning(warning.Code, warning.Message, warning.Context);
                    }
                }

                if (dto.ThumbnailPackObject != null)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiThumbnailPackNotMigrated.Code,
                        "thumbnailPackObject は未移行です。custom icon / previewImage のみ PoseTune icon に移行します。",
                        dto.ThumbnailPackObject);
                }
            }

            ReportSourcePolicyLimitations(plan.Dtos, options, report);
            CountPlan(plan.Dtos, options, report);
            if (plan.Dtos.All(dto => dto.Layers.Count == 0 && dto.Overrides.Count == 0))
            {
                report.Error(
                    PoseTuneDiagnostics.KawaiiSerializedReadWarning.Code,
                    "移行可能な layer または override がありません。移行元 schema を確認してください。",
                    plan.AvatarRoot);
            }
            ValidateTarget(plan);
            ValidateSources(plan);
            ValidateMotions(plan);
            ValidateAssetDestination(plan);
            report.Info(
                PoseTuneDiagnostics.KawaiiMigrationOptionsInfo.Code,
                "Options: " + KawaiiMigrationOptionSupport.Summary(options),
                plan.AvatarRoot);
            return plan;
        }

        private static void ValidateTarget(KawaiiMigrationPlan plan)
        {
            var options = plan.Options;
            if (options.createNewPoseTuneRoot)
            {
                var existingRoots = plan.Avatar.GetComponentsInChildren<PoseTuneRoot>(true);
                if (existingRoots.Length > 0)
                {
                    plan.Report.Error(
                        TargetMismatchCode,
                        "選択 Avatar には既存 PoseTuneRoot があります。新規 Root を作成せず、既存 Root を明示的に選択してください。",
                        existingRoots[0]);
                }
            }

            if (!options.createNewPoseTuneRoot && options.existingRoot == null)
            {
                plan.Report.Error(TargetMismatchCode, "既存 Root を使用する設定ですが、target が指定されていません。", plan.AvatarRoot);
                return;
            }

            if (!options.createNewPoseTuneRoot && options.existingRoot != null)
            {
                var owner = options.existingRoot.GetComponentInParent<VRCAvatarDescriptor>(true);
                if (owner != plan.Avatar)
                {
                    plan.Report.Error(TargetMismatchCode, "既存 PoseTuneRoot は選択 Avatar に属していません。", options.existingRoot);
                }

                if (EditorUtility.IsPersistent(options.existingRoot) || PrefabUtility.IsPartOfImmutablePrefab(options.existingRoot))
                {
                    plan.Report.Error(TargetMismatchCode, "既存 PoseTuneRoot は現在の Scene または Prefab Stage で編集できません。", options.existingRoot);
                }
            }

        }

        private static void ValidateSources(KawaiiMigrationPlan plan)
        {
            var sourceSet = new HashSet<MonoBehaviour>(plan.Sources);
            foreach (var source in plan.Sources)
            {
                var owner = source.GetComponentInParent<VRCAvatarDescriptor>(true);
                if (owner != plan.Avatar)
                {
                    plan.Report.Error(SourceScopeCode, "移行元 component は選択 Avatar に属していません。", source);
                    continue;
                }

                if (plan.Options.sourceDisposition == KawaiiSourceDisposition.KeepUnchanged)
                {
                    continue;
                }

                if (source.gameObject == plan.AvatarRoot)
                {
                    plan.Report.Error(
                        SourceRootMutationCode,
                        "Avatar Root GameObject 自体を EditorOnly または inactive にすることはできません。",
                        source);
                    continue;
                }

                if (!plan.Options.createNewPoseTuneRoot &&
                    plan.Options.existingRoot != null &&
                    (source.gameObject == plan.Options.existingRoot.gameObject ||
                     plan.Options.existingRoot.transform.IsChildOf(source.transform)))
                {
                    plan.Report.Error(
                        SourceRootMutationCode,
                        "移行元 GameObject 自体またはその子に target PoseTuneRoot があるため、移行元全体を変更できません。",
                        source);
                    continue;
                }

                if (IsSharedSourceObject(source.gameObject, sourceSet) &&
                    !plan.Options.confirmSharedSourceObjectMutation)
                {
                    plan.Report.Error(
                        SharedSourceConfirmationCode,
                        "共有 GameObject 全体への変更が確認されていません: " + PoseTuneHierarchyUtility.HierarchyPath(source.transform),
                        source);
                }
            }

            if (plan.Options.sourceDisposition == KawaiiSourceDisposition.KeepUnchanged)
            {
                plan.Report.Warning(
                    PoseTuneDiagnostics.KawaiiActiveSourceSystemRisk.Code,
                    "移行元を保持します。KawaiiPosing と PoseTune の両方が build に作用し得るため、build 前に構成を確認してください。",
                    plan.AvatarRoot);
            }
        }

        private static void ValidateMotions(KawaiiMigrationPlan plan)
        {
            foreach (var dto in plan.Dtos)
            {
                foreach (var layer in dto.Layers)
                {
                    foreach (var source in KawaiiPosingMapper.ImportableAnimations(layer, plan.Options, plan.Report))
                    {
                        ReportMissingBuildMotion(
                            plan,
                            dto,
                            source,
                            "build 対象 pose の animation clip が見つかりません: " + KawaiiPosingMapper.DisplayName(source));
                    }
                }

                foreach (var item in dto.Overrides)
                {
                    if (!KawaiiOverrideAuthoringWriter.ShouldImport(item, plan.Options))
                    {
                        continue;
                    }

                    foreach (var source in KawaiiOverrideAuthoringWriter.ExpandAnimations(item, plan.Options))
                    {
                        ReportMissingBuildMotion(
                            plan,
                            dto,
                            source,
                            "build 対象 override の Motion が見つかりません: " + item.StateType);
                    }
                }
            }
        }

        private static void ReportMissingBuildMotion(
            KawaiiMigrationPlan plan,
            KawaiiPosingSystemDto dto,
            KawaiiAnimationDto source,
            string message)
        {
            var resolved = KawaiiPoseAuthoringWriter.ResolveSourceMotion(source.Motion, source.Clip, plan.Options, null);
            var skippedBlendTree = KawaiiPoseAuthoringWriter.IsSkippedBlendTreeWithoutFallback(
                source,
                plan.Options,
                resolved);
            if (source.Enabled && !skippedBlendTree && resolved == null)
            {
                plan.Report.Error(PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code, message, dto.SourceComponent);
            }
        }

        private static void ValidateAssetDestination(KawaiiMigrationPlan plan)
        {
            if (!KawaiiMigrationAssetPathUtility.TryMigrationRoot(
                    plan.AvatarRoot,
                    "preflight",
                    out var path))
            {
                plan.Report.Error(
                    AssetPreflightCode,
                    "Kawaii migrationでAssetを生成するには、保存済みSceneまたはPrefab上のAvatarが必要です。",
                    plan.AvatarRoot);
                return;
            }

            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains(".."))
            {
                plan.Report.Error(AssetPreflightCode, "生成 asset path が Assets 配下の安全な path ではありません: " + path, plan.AvatarRoot);
                return;
            }

            var segments = path.Split('/');
            var existingParent = "Assets";
            for (var index = 1; index < segments.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(segments[index]))
                {
                    plan.Report.Error(AssetPreflightCode, "生成 asset path に空の segment があります: " + path, plan.AvatarRoot);
                    return;
                }

                var next = existingParent + "/" + segments[index];
                if (AssetDatabase.IsValidFolder(next))
                {
                    existingParent = next;
                    continue;
                }

                if (AssetDatabase.LoadMainAssetAtPath(next) != null)
                {
                    plan.Report.Error(AssetPreflightCode, "生成先 folder と同名の asset が存在します: " + next, plan.AvatarRoot);
                    return;
                }

                break;
            }

            if (!AssetDatabase.IsOpenForEdit(existingParent))
            {
                plan.Report.Error(AssetPreflightCode, "生成先 asset folder は編集できません: " + existingParent, plan.AvatarRoot);
            }
        }

        internal static bool IsSharedSourceObject(GameObject gameObject, ISet<MonoBehaviour> selectedSources)
        {
            if (gameObject == null || gameObject.transform.childCount > 0)
            {
                return true;
            }

            return gameObject.GetComponents<Component>()
                .Any(component => IsUnrelatedSourceComponent(component, selectedSources));
        }

        internal static bool IsUnrelatedSourceComponent(Component component, ISet<MonoBehaviour> selectedSources)
        {
            return component is not Transform &&
                   (component is not MonoBehaviour behaviour || !selectedSources.Contains(behaviour));
        }

        private static void ReportSourcePolicyLimitations(
            IEnumerable<KawaiiPosingSystemDto> dtos,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            foreach (var dto in dtos ?? Enumerable.Empty<KawaiiPosingSystemDto>())
            {
                if (dto.MergeTrackingControl)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiControllerTrackingMergeNotStrictlyMigrated.Code,
                        "source mergeTrackingControl による全 controller の tracking control 統合は厳密移行できません。addTrackingPolicy 有効時は PoseTune の group kind policy へ近似します。",
                        dto.SourceComponent);
                }

                if (options.addTrackingPolicy &&
                    dto.Overrides.Any(item => KawaiiOverrideAuthoringWriter.ShouldImport(item, options)))
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiOverrideTrackingNotStrictlyMigrated.Code,
                        "override の Animator state に含まれる tracking control は source definition から厳密復元できません。PoseTune の group kind policy へ近似します。",
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

                foreach (var item in dto.Overrides)
                {
                    if (!KawaiiOverrideAuthoringWriter.ShouldImport(item, options))
                    {
                        continue;
                    }

                    var expanded = KawaiiOverrideAuthoringWriter.ExpandAnimations(item, options);
                    report.CreatedGroupCount++;
                    report.CreatedPoseCount += expanded.Count;
                    if (item.BlendTree != null)
                    {
                        report.BlendTreePoseCount++;
                    }
                }
            }
        }
    }
}
