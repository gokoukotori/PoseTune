using System;
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
                    report,
                    UndoName);
                if (root == null)
                {
                    throw new KawaiiMigrationAbortException("PoseTuneRoot could not be prepared.");
                }

                assetStore = assetStoreFactory(plan.AvatarRoot, root, report) ??
                             throw new InvalidOperationException("Kawaii migration asset store factory returned null.");
                var groupsRoot = KawaiiRootAuthoringWriter.EnsureGroupsRoot(root, report, UndoName);

                WriteAuthoring(plan, groupsRoot, assetStore);

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
                RollBackFailedRun(plan, undoGroup, assetStore, exception);
                return report;
            }
        }

        private static void WriteAuthoring(
            KawaiiMigrationPlan plan,
            GameObject groupsRoot,
            IKawaiiMigrationAssetStore assetStore)
        {
            var report = plan.Report;
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
        }

        private static void RollBackFailedRun(
            KawaiiMigrationPlan plan,
            int undoGroup,
            IKawaiiMigrationAssetStore assetStore,
            Exception exception)
        {
            var report = plan.Report;
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
        }

        internal KawaiiMigrationPlan CreatePlan(
            GameObject avatarRoot,
            IEnumerable<MonoBehaviour> sourceComponents,
            KawaiiMigrationOptions options)
        {
            return KawaiiMigrationPlanner.CreatePlan(avatarRoot, sourceComponents, options);
        }

        private void ValidateCommittedAuthoring(
            KawaiiMigrationPlan plan,
            PoseTuneRoot root,
            IKawaiiMigrationAssetStore assetStore,
            KawaiiMigrationReport report)
        {
            if (root.GetComponentInParent<VRCAvatarDescriptor>(true) != plan.Avatar)
            {
                report.Error(KawaiiMigrationPlanner.TargetMismatchCode, "生成した PoseTuneRoot が選択 Avatar 外にあります。", root);
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
            return KawaiiMigrationPlanner.IsSharedSourceObject(gameObject, selectedSources);
        }

        internal static bool IsUnrelatedSourceComponent(Component component, ISet<MonoBehaviour> selectedSources)
        {
            return KawaiiMigrationPlanner.IsUnrelatedSourceComponent(component, selectedSources);
        }

        private sealed class KawaiiMigrationAbortException : Exception
        {
            public KawaiiMigrationAbortException(string message) : base(message)
            {
            }
        }
    }
}
