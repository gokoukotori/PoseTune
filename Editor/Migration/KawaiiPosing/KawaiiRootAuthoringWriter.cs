using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiRootAuthoringWriter
    {
        public static PoseTuneRoot PrepareRoot(
            GameObject avatarRoot,
            KawaiiMigrationOptions options,
            IReadOnlyList<KawaiiPosingSystemDto> dtos,
            KawaiiMigrationReport report,
            string undoName)
        {
            var root = ResolveTargetRoot(avatarRoot, options, report, undoName);
            ApplyRootOptions(root, options, dtos, report, undoName);
            EnsureGroupsRoot(root, report, undoName);
            EnsureMenu(root, report, undoName);
            KawaiiHeightAuthoringWriter.EnsureHeight(root, options, report, undoName);
            EnsureTrackingPolicy(root, options, report);
            EnsurePoseOptions(root, report, undoName);
            return root;
        }

        public static GameObject EnsureGroupsRoot(
            PoseTuneRoot root,
            KawaiiMigrationReport report,
            string undoName)
        {
            return KawaiiAuthoringObjectUtility.EnsureChild(root.transform, "ポーズグループ", report, "Container", undoName);
        }

        private static PoseTuneRoot ResolveTargetRoot(
            GameObject avatarRoot,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            string undoName)
        {
            if (!options.createNewPoseTuneRoot && options.existingRoot != null)
            {
                return options.existingRoot;
            }

            var rootObject = new GameObject("PoseTune Kawaii Migration");
            Undo.RegisterCreatedObjectUndo(rootObject, undoName);
            Undo.SetTransformParent(rootObject.transform, KawaiiAuthoringObjectUtility.ResolveAvatar(avatarRoot).transform, undoName);
            var root = Undo.AddComponent<PoseTuneRoot>(rootObject);
            Undo.AddComponent<PoseTuneAssistant>(rootObject);
            report.Created(rootObject, "Root");
            return root;
        }

        private static void ApplyRootOptions(
            PoseTuneRoot root,
            KawaiiMigrationOptions options,
            IReadOnlyList<KawaiiPosingSystemDto> dtos,
            KawaiiMigrationReport report,
            string undoName)
        {
            Undo.RecordObject(root, undoName);
            root.targetLayer = options.targetLayerMode == KawaiiTargetLayerMode.ActionApproximate
                ? PoseTuneTargetLayer.Action
                : PoseTuneTargetLayer.Base;
            if (options.targetLayerMode == KawaiiTargetLayerMode.ActionApproximate)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiActionLayerApproximation.Code, "Action layer mode は Kawaii Base layer 互換より近似度が下がります。", root);
            }

            root.enableAutoContextSwitch = options.enableAutoContextSwitch;
            root.defaultMode = options.enableAutoContextSwitch
                ? PoseTuneDefaultMode.Auto
                : PoseTuneDefaultMode.Manual;
            root.poseSelectionSyncMode = options.selectionSyncMode;
            root.poseWriteDefaultsMode = PoseWriteDefaultsMode.ForceOff;
            root.disableWhenFullBodyTracking = options.disableWhenFullBodyTracking;
            root.enableHeightAdjust = options.footHeightMode != KawaiiFootHeightMode.Off;
            root.advancedSettings.allowFullBodyTracking = !options.disableWhenFullBodyTracking;
            root.enableIconGeneration = dtos == null || dtos.Any(dto => !dto.IsIconDisabled);
            if (dtos != null && dtos.Any(dto => dto.IsIconSmall))
            {
                root.previewSettings.thumbnailSize = 64;
            }

            if (options.selectionSyncMode == PoseSelectionSyncMode.DirectGroupParameter)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiSyncedParameterDirectGroupApproximation.Code, "Kawaii syncdParameterValue は保持されますが、現在の同期方式は group Int 直接同期です。圧縮 Pose ID 互換ではありません。", root);
            }

            report.Warning(PoseTuneDiagnostics.KawaiiPoseThresholdApproximation.Code, "Kawaii の姿勢切替閾値は PoseTune 固定閾値へ近似されます。", root);
            if (options.rootRecenterMode == KawaiiRootRecenterMode.FirstRootKeyApproximation)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiRootRecenterApproximation.Code, "Root recenter は first RootT key 基準の近似です。Kawaii の Head XZ sampling とは完全一致しません。", root);
            }

            EditorUtility.SetDirty(root);
            KawaiiAuthoringObjectUtility.RecordPrefabModifications(root);
        }

        private static void EnsureMenu(PoseTuneRoot root, KawaiiMigrationReport report, string undoName)
        {
            var existing = root.GetComponentsInChildren<PoseMenu>(true)
                .FirstOrDefault(menu => menu.GetComponentInParent<PoseTuneRoot>(true) == root);
            if (existing != null)
            {
                Undo.RecordObject(existing, undoName);
                ((Behaviour)existing).enabled = true;
                existing.lyingMenuLayout = LyingMenuLayout.SeparateGroups;
                existing.generateIcons = root.enableIconGeneration;
                EditorUtility.SetDirty(existing);
                KawaiiAuthoringObjectUtility.RecordPrefabModifications(existing);
                return;
            }

            var menu = KawaiiAuthoringObjectUtility.EnsureChild(root.transform, "メニュー", report, "Menu", undoName);
            var poseMenu = menu.GetComponent<PoseMenu>() ?? Undo.AddComponent<PoseMenu>(menu);
            Undo.RecordObject(poseMenu, undoName);
            poseMenu.lyingMenuLayout = LyingMenuLayout.SeparateGroups;
            poseMenu.generateIcons = root.enableIconGeneration;
            EditorUtility.SetDirty(poseMenu);
            KawaiiAuthoringObjectUtility.RecordPrefabModifications(poseMenu);
        }

        private static void EnsureTrackingPolicy(
            PoseTuneRoot root,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            if (!options.addTrackingPolicy || root.GetComponent<PoseTrackingPolicy>() != null)
            {
                return;
            }

            var policy = Undo.AddComponent<PoseTrackingPolicy>(root.gameObject);
            policy.tracking = TrackingPolicyData.DefaultForPose();
            report.Created(policy, "TrackingPolicy");
        }

        private static void EnsurePoseOptions(PoseTuneRoot root, KawaiiMigrationReport report, string undoName)
        {
            var existing = root.GetComponentsInChildren<PoseOption>(true)
                .FirstOrDefault(options => options.GetComponentInParent<PoseTuneRoot>(true) == root);
            if (existing != null)
            {
                Undo.RecordObject(existing, undoName);
                ((Behaviour)existing).enabled = true;
                existing.options.locomotionLock = false;
                EditorUtility.SetDirty(existing);
                KawaiiAuthoringObjectUtility.RecordPrefabModifications(existing);
                return;
            }

            var optionsObject = KawaiiAuthoringObjectUtility.EnsureChild(root.transform, "オプション", report, "Options", undoName);
            var options = optionsObject.GetComponent<PoseOption>() ?? Undo.AddComponent<PoseOption>(optionsObject);
            Undo.RecordObject(options, undoName);
            options.options.locomotionLock = false;
            EditorUtility.SetDirty(options);
            KawaiiAuthoringObjectUtility.RecordPrefabModifications(options);
        }
    }
}
