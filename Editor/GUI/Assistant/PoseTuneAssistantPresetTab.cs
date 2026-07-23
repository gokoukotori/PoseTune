using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantPresetTab
    {
        private static readonly Dictionary<int, PoseTunePreset> PresetSelections = new();
        private static readonly Dictionary<int, AvatarAdjustmentPreset> AdjustmentPresetSelections = new();

        public static void Draw(PoseTuneRoot root)
        {
            var id = root.GetInstanceID();
            PresetSelections.TryGetValue(id, out var preset);
            AdjustmentPresetSelections.TryGetValue(id, out var adjustmentPreset);

            preset = (PoseTunePreset)EditorGUILayout.ObjectField("PoseTunePreset", preset, typeof(PoseTunePreset), false);
            PresetSelections[id] = preset;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("現在の構成をプリセットへ保存"))
                {
                    preset = SavePosePreset(root, preset);
                    PresetSelections[id] = preset;
                }

                using (new EditorGUI.DisabledScope(preset == null))
                {
                    if (GUILayout.Button("Merge 適用"))
                    {
                        ApplyPosePreset(root, preset, PoseTunePresetApplyMode.Merge);
                    }

                    if (GUILayout.Button("Replace 適用"))
                    {
                        ApplyPosePreset(root, preset, PoseTunePresetApplyMode.Replace);
                    }
                }
            }

            EditorGUILayout.Space();
            adjustmentPreset = (AvatarAdjustmentPreset)EditorGUILayout.ObjectField(
                "AvatarAdjustmentPreset",
                adjustmentPreset,
                typeof(AvatarAdjustmentPreset),
                false);
            AdjustmentPresetSelections[id] = adjustmentPreset;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("現在の調整を保存"))
                {
                    adjustmentPreset = SaveAdjustmentPreset(root, adjustmentPreset);
                    AdjustmentPresetSelections[id] = adjustmentPreset;
                }

                using (new EditorGUI.DisabledScope(adjustmentPreset == null))
                {
                    if (GUILayout.Button("調整を適用"))
                    {
                        var report = new PoseTunePresetApplier().ApplyAdjustments(root, adjustmentPreset);
                        foreach (var issue in report.Warnings)
                        {
                            PoseTuneLog.Warning($"{issue.Code}: {issue.Message}", issue.Context);
                        }
                    }
                }
            }
        }

        private static void ApplyPosePreset(
            PoseTuneRoot root,
            PoseTunePreset preset,
            PoseTunePresetApplyMode mode)
        {
            var applier = new PoseTunePresetApplier();
            var plan = applier.CreatePlan(root, preset, mode);
            if (!plan.IsValid)
            {
                foreach (var error in plan.Errors)
                {
                    PoseTuneLog.Error("Preset apply aborted: " + error, root);
                }

                EditorUtility.DisplayDialog(
                    "PoseTune",
                    "プリセットを適用できません。Console のエラーを確認してください。",
                    "OK");
                return;
            }

            if (mode == PoseTunePresetApplyMode.Replace &&
                (plan.RemovedGroupCount > 0 || plan.RemovedPoseCount > 0 || plan.RemovedDependentComponentCount > 0) &&
                !EditorUtility.DisplayDialog(
                    "PoseTune Replace",
                    $"プリセットにない Group {plan.RemovedGroupCount} 件、Pose {plan.RemovedPoseCount} 件を削除します。\n" +
                    $"所有者がなくなる PoseCondition / PoseTrackingPolicy {plan.RemovedDependentComponentCount} 件も削除します。\n" +
                    "GameObject とそれ以外の Component は保持されます。続行しますか？",
                    "Replace",
                    "キャンセル"))
            {
                return;
            }

            if (!applier.Commit(plan))
            {
                EditorUtility.DisplayDialog(
                    "PoseTune",
                    "プリセット適用中にエラーが発生したため、変更をロールバックしました。",
                    "OK");
            }
        }

        private static PoseTunePreset SavePosePreset(PoseTuneRoot root, PoseTunePreset target)
        {
            var captured = new PoseTunePresetApplier().Capture(root);
            target ??= PoseTuneAssistantAssetFactory.Create<PoseTunePreset>(root, "PoseTunePreset");
            EditorUtility.CopySerialized(captured, target);
            Object.DestroyImmediate(captured);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            return target;
        }

        private static AvatarAdjustmentPreset SaveAdjustmentPreset(PoseTuneRoot root, AvatarAdjustmentPreset target)
        {
            var avatar = root.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
            var captured = new PoseTunePresetApplier().CaptureAdjustments(root, avatar != null ? avatar.gameObject : null);
            target ??= PoseTuneAssistantAssetFactory.Create<AvatarAdjustmentPreset>(root, "AvatarAdjustmentPreset");
            EditorUtility.CopySerialized(captured, target);
            Object.DestroyImmediate(captured);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            return target;
        }
    }
}
