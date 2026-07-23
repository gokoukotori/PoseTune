using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
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
        internal string PlannedRootStableGuid = "";

        internal bool IsValid => Report != null && !Report.HasErrors;
    }

    internal sealed class KawaiiPosingMigrationExecutor
    {
        private const string UndoName = "KawaiiPosing から PoseTune へ移行";
        private const string TargetMismatchCode = "PT-K005";
        private const string SourceScopeCode = "PT-K006";
        private const string SourceRootMutationCode = "PT-K007";
        private const string SharedSourceConfirmationCode = "PT-K008";
        private const string AssetPreflightCode = "PT-K009";
        private const string RollbackCode = "PT-K010";
        private const string AssetPersistenceCode = "PT-K011";

        private readonly Func<GameObject, PoseTuneRoot, KawaiiMigrationReport, IKawaiiMigrationAssetStore> assetStoreFactory;
        private readonly Func<PoseTuneRoot, ValidationReport> finalValidator;

        internal KawaiiPosingMigrationExecutor(
            Func<GameObject, PoseTuneRoot, KawaiiMigrationReport, IKawaiiMigrationAssetStore> assetStoreFactory = null,
            Func<PoseTuneRoot, ValidationReport> finalValidator = null)
        {
            this.assetStoreFactory = assetStoreFactory ??
                                     ((avatar, root, report) => new KawaiiMigrationAssetStore(avatar, root, report));
            this.finalValidator = finalValidator ?? (root =>
                new PoseValidator().Validate(new PoseGraphCollector().Collect(root)));
        }

        public KawaiiMigrationReport Execute(
            GameObject avatarRoot,
            IEnumerable<MonoBehaviour> sourceComponents,
            KawaiiMigrationOptions options)
        {
            var plan = CreatePlan(avatarRoot, sourceComponents, options);
            var report = plan.Report;
            if (plan.Options.dryRunOnly || !plan.IsValid)
            {
                report.Info(
                    PoseTuneDiagnostics.KawaiiMigrationSummaryInfo.Code,
                    $"Dry-run: groups={report.CreatedGroupCount}, poses={report.CreatedPoseCount}",
                    avatarRoot);
                return report;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            IKawaiiMigrationAssetStore assetStore = null;
            try
            {
                var root = KawaiiRootAuthoringWriter.PrepareRoot(
                    plan.AvatarRoot,
                    plan.Options,
                    plan.Dtos,
                    report,
                    UndoName);
                if (root == null)
                {
                    throw new KawaiiMigrationAbortException("PoseTuneRoot could not be prepared.");
                }

                if (string.IsNullOrEmpty(ReadStableGuid(root)))
                {
                    Undo.RecordObject(root, UndoName);
                    root.SetStableGuid(plan.PlannedRootStableGuid);
                    KawaiiAuthoringObjectUtility.RecordPrefabModifications(root);
                }

                EnsureOwnedStableGuids(root);

                assetStore = assetStoreFactory(plan.AvatarRoot, root, report) ??
                             throw new InvalidOperationException("Kawaii migration asset store factory returned null.");
                var groupsRoot = KawaiiRootAuthoringWriter.EnsureGroupsRoot(root, report, UndoName);

                foreach (var dto in plan.Dtos)
                {
                    foreach (var layer in dto.Layers.OrderBy(layer => layer.Index))
                    {
                        KawaiiGroupAuthoringWriter.CreateLayer(
                            groupsRoot.transform,
                            dto,
                            layer,
                            plan.Options,
                            report,
                            UndoName,
                            assetStore);
                    }

                    KawaiiOverrideAuthoringWriter.ImportOverrides(
                        groupsRoot.transform,
                        dto,
                        plan.Options,
                        report,
                        UndoName,
                        assetStore);
                }

                if (report.HasErrors)
                {
                    throw new KawaiiMigrationAbortException("Migration writer reported an error.");
                }

                ValidateCommittedAuthoring(plan, root, assetStore, report);
                if (report.HasErrors)
                {
                    throw new KawaiiMigrationAbortException("Final migration validation failed.");
                }

                assetStore.CommitManifest(plan.Sources, plan.Options);
                PostprocessSources(plan.Sources, plan.Options);
                KawaiiAuthoringObjectUtility.RecordPrefabModifications(root);
                Undo.CollapseUndoOperations(undoGroup);
                report.Info(
                    PoseTuneDiagnostics.KawaiiMigrationSummaryInfo.Code,
                    $"Kawaii migration completed: groups={report.CreatedGroupCount}, poses={report.CreatedPoseCount}",
                    plan.AvatarRoot);
                return report;
            }
            catch (Exception exception)
            {
                try
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                }
                catch (Exception undoRollbackException)
                {
                    report.Error(
                        RollbackCode,
                        "Scene rollback failed. Manual cleanup may be required: " + undoRollbackException.Message,
                        plan.AvatarRoot);
                    Debug.LogException(undoRollbackException, plan.AvatarRoot);
                }

                try
                {
                    assetStore?.Rollback();
                }
                catch (Exception rollbackException)
                {
                    report.Error(
                        RollbackCode,
                        "Asset rollback failed. Manual cleanup may be required: " + rollbackException.Message,
                        plan.AvatarRoot);
                    Debug.LogException(rollbackException, plan.AvatarRoot);
                }

                if (exception is not KawaiiMigrationAbortException)
                {
                    Debug.LogException(exception, plan.AvatarRoot);
                }

                report.Error(RollbackCode, "Kawaii migration was rolled back: " + exception.Message, plan.AvatarRoot);
                report.CreatedObjects.Clear();
                return report;
            }
        }

        internal KawaiiMigrationPlan CreatePlan(
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

            plan.Avatar = ResolveAvatar(avatarRoot);
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

            var currentGuid = !options.createNewPoseTuneRoot && options.existingRoot != null
                ? ReadStableGuid(options.existingRoot)
                : "";
            var parsedGuid = Guid.Empty;
            if (!string.IsNullOrEmpty(currentGuid) && !Guid.TryParse(currentGuid, out parsedGuid))
            {
                plan.Report.Error(TargetMismatchCode, "既存 PoseTuneRoot の Stable GUID が不正です。", options.existingRoot);
                plan.PlannedRootStableGuid = "";
                return;
            }

            plan.PlannedRootStableGuid = !string.IsNullOrEmpty(currentGuid)
                ? parsedGuid.ToString("N")
                : Guid.NewGuid().ToString("N");
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
                        "共有 GameObject 全体への変更が確認されていません: " + HierarchyPath(source.transform),
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
                        var resolved = ResolveSourceMotionWithoutReporting(source, plan.Options);
                        var skippedBlendTree = KawaiiPoseAuthoringWriter.IsSkippedBlendTreeWithoutFallback(
                            source,
                            plan.Options,
                            resolved);
                        if (source.Enabled && !skippedBlendTree && resolved == null)
                        {
                            plan.Report.Error(
                                PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code,
                                "build 対象 pose の animation clip が見つかりません: " + KawaiiPosingMapper.DisplayName(source),
                                dto.SourceComponent);
                        }
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
                        var resolved = ResolveSourceMotionWithoutReporting(source, plan.Options);
                        var skippedBlendTree = KawaiiPoseAuthoringWriter.IsSkippedBlendTreeWithoutFallback(
                            source,
                            plan.Options,
                            resolved);
                        if (source.Enabled && !skippedBlendTree && resolved == null)
                        {
                            plan.Report.Error(
                                PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code,
                                "build 対象 override の Motion が見つかりません: " + item.StateType,
                                dto.SourceComponent);
                        }
                    }
                }
            }
        }

        private static Motion ResolveSourceMotionWithoutReporting(
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options)
        {
            if (source.Motion is BlendTree)
            {
                return options.blendTreeMode switch
                {
                    KawaiiBlendTreeMode.Skip => null,
                    KawaiiBlendTreeMode.FlattenLeaves => source.Clip != null ? source.Clip : source.Motion,
                    _ => source.Motion
                };
            }

            return source.Motion != null ? source.Motion : source.Clip;
        }

        private static void ValidateAssetDestination(KawaiiMigrationPlan plan)
        {
            if (string.IsNullOrEmpty(plan.PlannedRootStableGuid))
            {
                return;
            }

            var path = KawaiiMigrationAssetPathUtility.MigrationRoot(
                plan.AvatarRoot.name,
                plan.PlannedRootStableGuid);
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

        private void ValidateCommittedAuthoring(
            KawaiiMigrationPlan plan,
            PoseTuneRoot root,
            IKawaiiMigrationAssetStore assetStore,
            KawaiiMigrationReport report)
        {
            if (root.GetComponentInParent<VRCAvatarDescriptor>(true) != plan.Avatar)
            {
                report.Error(TargetMismatchCode, "生成した PoseTuneRoot が選択 Avatar 外にあります。", root);
            }

            var createdPoses = report.CreatedObjects
                .SelectMany(created => created.Object is GameObject gameObject
                    ? gameObject.GetComponents<PoseClip>()
                    : created.Object is PoseClip pose
                        ? new[] { pose }
                        : Array.Empty<PoseClip>())
                .Where(pose => pose != null)
                .Distinct();
            foreach (var pose in createdPoses)
            {
                if (pose.includeInBuild && pose.sourceMotion == null && pose.clip == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code,
                        "commit 後の build 対象 pose に Motion がありません: " + pose.displayName,
                        pose);
                }
            }

            foreach (var pose in assetStore.BakedPoses.Where(pose => pose != null))
            {
                if (pose.sourceMotion == null || !EditorUtility.IsPersistent(pose.sourceMotion))
                {
                    report.Error(
                        AssetPersistenceCode,
                        "BakeAtMigration Motion が永続 asset ではありません: " + pose.displayName,
                        pose);
                }
            }

            var validation = finalValidator(root) ?? new ValidationReport();
            foreach (var issue in validation.Errors)
            {
                report.Error(issue.Code, issue.Message, issue.Context != null ? issue.Context : root);
            }

            foreach (var issue in validation.Warnings)
            {
                if (plan.Options.sourceDisposition != KawaiiSourceDisposition.KeepUnchanged &&
                    string.Equals(
                        issue.Code,
                        PoseTuneDiagnostics.KawaiiActiveSourceSystemRisk.Code,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                report.Warning(issue.Code, issue.Message, issue.Context != null ? issue.Context : root);
            }
        }

        private static void EnsureOwnedStableGuids(PoseTuneRoot root)
        {
            foreach (var group in root.GetComponentsInChildren<PoseGroup>(true)
                         .Where(group => group.GetComponentInParent<PoseTuneRoot>(true) == root))
            {
                EnsureStableGuidWithUndo(group);
            }

            foreach (var pose in root.GetComponentsInChildren<PoseClip>(true)
                         .Where(pose => pose.GetComponentInParent<PoseTuneRoot>(true) == root))
            {
                EnsureStableGuidWithUndo(pose);
            }

            Undo.FlushUndoRecordObjects();
        }

        private static void EnsureStableGuidWithUndo(UnityEngine.Object component)
        {
            using var serialized = new SerializedObject(component);
            var property = serialized.FindProperty("stableGuid.value");
            if (property == null || !string.IsNullOrWhiteSpace(property.stringValue))
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(component, UndoName);
            property.stringValue = Guid.NewGuid().ToString("N");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            KawaiiAuthoringObjectUtility.RecordPrefabModifications(component);
        }

        private static void PostprocessSources(
            IEnumerable<MonoBehaviour> sources,
            KawaiiMigrationOptions options)
        {
            if (options.sourceDisposition == KawaiiSourceDisposition.KeepUnchanged)
            {
                return;
            }

            var seenObjects = new HashSet<int>();
            foreach (var source in sources.Where(source => source != null))
            {
                var gameObject = source.gameObject;
                if (!seenObjects.Add(gameObject.GetInstanceID()))
                {
                    continue;
                }

                Undo.RecordObject(gameObject, UndoName);
                switch (options.sourceDisposition)
                {
                    case KawaiiSourceDisposition.MarkGameObjectEditorOnly:
                        gameObject.tag = "EditorOnly";
                        break;
                    case KawaiiSourceDisposition.DeactivateGameObject:
                        gameObject.SetActive(false);
                        break;
                }

                KawaiiAuthoringObjectUtility.RecordPrefabModifications(gameObject);
            }
        }

        internal static bool IsSharedSourceObject(GameObject gameObject, ISet<MonoBehaviour> selectedSources)
        {
            if (gameObject == null || gameObject.transform.childCount > 0)
            {
                return true;
            }

            return gameObject.GetComponents<Component>().Any(component =>
                component is not Transform &&
                (component is not MonoBehaviour behaviour || !selectedSources.Contains(behaviour)));
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

        private static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            return selected != null
                ? selected.GetComponent<VRCAvatarDescriptor>()
                  ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                  ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true)
                : null;
        }

        private static string ReadStableGuid(PoseTuneRoot root)
        {
            return ReadStableGuid((UnityEngine.Object)root);
        }

        private static string ReadStableGuid(UnityEngine.Object component)
        {
            if (component == null)
            {
                return "";
            }

            using var serialized = new SerializedObject(component);
            return serialized.FindProperty("stableGuid.value")?.stringValue?.Trim() ?? "";
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        private sealed class KawaiiMigrationAbortException : Exception
        {
            public KawaiiMigrationAbortException(string message) : base(message)
            {
            }
        }
    }
}
