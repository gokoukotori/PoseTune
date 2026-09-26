using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiPresetAdjustmentSyncWindow : EditorWindow
    {
        private PoseTuneRoot root;
        private Preset preset;
        private Vector2 scroll;
        private KawaiiPresetAdjustmentSyncPlan lastPlan;
        private string resultMessage = "";
        private MessageType resultMessageType = MessageType.None;

        public static void Open(PoseTuneRoot suggestedRoot)
        {
            var window = GetWindow<KawaiiPresetAdjustmentSyncWindow>("KawaiiPosing 調整同期");
            window.root = suggestedRoot;
            window.InvalidatePlan();
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += InvalidatePlan;
            EditorApplication.projectChanged += InvalidatePlan;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= InvalidatePlan;
            EditorApplication.projectChanged -= InvalidatePlan;
        }

        private void OnGUI()
        {
            using var scrollView = new EditorGUILayout.ScrollViewScope(scroll);
            scroll = scrollView.scrollPosition;

            EditorGUI.BeginChangeCheck();
            root = (PoseTuneRoot)EditorGUILayout.ObjectField("対象 PoseTuneRoot", root, typeof(PoseTuneRoot), true);
            preset = (Preset)EditorGUILayout.ObjectField("Kawaii Preset", preset, typeof(Preset), false);
            if (EditorGUI.EndChangeCheck())
            {
                InvalidatePlan();
            }

            EditorGUILayout.HelpBox(
                "PresetのベースMotionとPoseClip.sourceMotionを完全一致で照合し、調整クリップを同期します。" +
                "Preset側が未設定の場合は既存値をクリアします。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(root == null || preset == null))
                {
                    if (GUILayout.Button("ドライラン"))
                    {
                        lastPlan = new KawaiiPresetAdjustmentSyncExecutor().CreatePlan(root, preset);
                        resultMessage = "ドライランを更新しました。";
                        resultMessageType = lastPlan.HasErrors ? MessageType.Error : MessageType.Info;
                    }
                }

                using (new EditorGUI.DisabledScope(lastPlan == null || lastPlan.HasErrors || !lastPlan.HasChanges))
                {
                    if (GUILayout.Button("同期を実行"))
                    {
                        ExecuteSync();
                    }
                }
            }

            if (!string.IsNullOrEmpty(resultMessage))
            {
                EditorGUILayout.HelpBox(resultMessage, resultMessageType);
            }

            DrawReport();
        }

        private void ExecuteSync()
        {
            var executor = new KawaiiPresetAdjustmentSyncExecutor();
            var currentPlan = executor.CreatePlan(root, preset);
            if (!string.Equals(lastPlan.Fingerprint, currentPlan.Fingerprint, StringComparison.Ordinal))
            {
                currentPlan.Warning(
                    KawaiiPresetAdjustmentSyncExecutor.StalePlanCode,
                    "入力またはPoseTuneの状態が変わりました。最新のドライラン結果を確認してください。",
                    root);
                lastPlan = currentPlan;
                resultMessage = "状態変更を検出したため同期しませんでした。";
                resultMessageType = MessageType.Warning;
                return;
            }

            lastPlan = currentPlan;
            if (currentPlan.HasErrors)
            {
                resultMessage = "最新のドライランにErrorがあるため同期しませんでした。";
                resultMessageType = MessageType.Error;
                return;
            }

            if (!currentPlan.HasChanges)
            {
                resultMessage = "同期が必要な調整クリップはありません。";
                resultMessageType = MessageType.Info;
                return;
            }

            var setCount = currentPlan.Count(KawaiiPresetAdjustmentSyncStatus.Set);
            var clearCount = currentPlan.Count(KawaiiPresetAdjustmentSyncStatus.Clear);
            if (!EditorUtility.DisplayDialog(
                    "PoseTune Kawaii Adjustment Sync",
                    $"調整クリップを同期します。\n\n設定: {setCount}\nクリア: {clearCount}\n\n続行しますか？",
                    "同期",
                    "キャンセル"))
            {
                return;
            }

            var result = executor.Execute(currentPlan);
            lastPlan = result.Plan;
            resultMessage = result.Message;
            resultMessageType = result.Status switch
            {
                KawaiiPresetAdjustmentExecutionStatus.Applied => MessageType.Info,
                KawaiiPresetAdjustmentExecutionStatus.NoChanges => MessageType.Info,
                KawaiiPresetAdjustmentExecutionStatus.Stale => MessageType.Warning,
                _ => MessageType.Error
            };
        }

        private void DrawReport()
        {
            if (lastPlan == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ドライラン結果", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Set={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.Set)}, " +
                $"Clear={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.Clear)}, " +
                $"NoChange={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.NoChange)}");
            EditorGUILayout.LabelField(
                $"Unmatched={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.Unmatched)}, " +
                $"Ambiguous={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.Ambiguous)}, " +
                $"Incompatible={lastPlan.Count(KawaiiPresetAdjustmentSyncStatus.Incompatible)}");

            foreach (var issue in lastPlan.Issues)
            {
                var messageType = issue.Severity switch
                {
                    KawaiiMigrationSeverity.Error => MessageType.Error,
                    KawaiiMigrationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                EditorGUILayout.HelpBox(issue.Code + ": " + issue.Message, messageType);
            }

            foreach (var entry in lastPlan.Entries)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(StatusLabel(entry.Status) + "  " + entry.DisplayName);
                    EditorGUILayout.LabelField(entry.Message, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.ObjectField("ベース Motion", entry.SourceMotion, typeof(Motion), false);
                    if (entry.Target != null)
                    {
                        EditorGUILayout.ObjectField("対象 PoseClip", entry.Target, typeof(PoseClip), true);
                    }
                    else
                    {
                        foreach (var candidate in entry.CandidateTargets)
                        {
                            EditorGUILayout.ObjectField("候補 PoseClip", candidate, typeof(PoseClip), true);
                        }
                    }

                    EditorGUILayout.ObjectField("同期する調整 Clip", entry.SourceAdjustmentClip, typeof(AnimationClip), false);
                }
            }
        }

        private static string StatusLabel(KawaiiPresetAdjustmentSyncStatus status)
        {
            return status switch
            {
                KawaiiPresetAdjustmentSyncStatus.Set => "[Set]",
                KawaiiPresetAdjustmentSyncStatus.Clear => "[Clear]",
                KawaiiPresetAdjustmentSyncStatus.NoChange => "[NoChange]",
                KawaiiPresetAdjustmentSyncStatus.Unmatched => "[Unmatched]",
                KawaiiPresetAdjustmentSyncStatus.Ambiguous => "[Ambiguous]",
                KawaiiPresetAdjustmentSyncStatus.Incompatible => "[Incompatible]",
                _ => "[Unknown]"
            };
        }

        private void InvalidatePlan()
        {
            lastPlan = null;
            resultMessage = "";
            resultMessageType = MessageType.None;
            Repaint();
        }
    }
}
