using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum KawaiiPresetAdjustmentSyncStatus
    {
        Set,
        Clear,
        NoChange,
        Unmatched,
        Ambiguous,
        Incompatible
    }

    internal enum KawaiiPresetAdjustmentExecutionStatus
    {
        Applied,
        Stale,
        NoChanges,
        Invalid,
        RolledBack
    }

    internal sealed class KawaiiPresetAdjustmentSyncEntry
    {
        public string SourceKey = "";
        public string DisplayName = "";
        public Motion SourceMotion;
        public AnimationClip SourceAdjustmentClip;
        public PoseClip Target;
        public PoseClip[] CandidateTargets = Array.Empty<PoseClip>();
        public KawaiiPresetAdjustmentSyncStatus Status;
        public string Message = "";
        public Object Context;
    }

    internal sealed class KawaiiPresetAdjustmentSyncIssue
    {
        public KawaiiMigrationSeverity Severity;
        public string Code = "";
        public string Message = "";
        public Object Context;
    }

    internal sealed class KawaiiPresetAdjustmentSyncPlan
    {
        public PoseTuneRoot Root;
        public Preset Preset;
        public readonly List<KawaiiPresetAdjustmentSyncEntry> Entries = new();
        public readonly List<KawaiiPresetAdjustmentSyncIssue> Issues = new();
        public string Fingerprint = "";

        public bool HasErrors => Issues.Any(issue => issue.Severity == KawaiiMigrationSeverity.Error);
        public bool HasChanges => Count(KawaiiPresetAdjustmentSyncStatus.Set) +
                                  Count(KawaiiPresetAdjustmentSyncStatus.Clear) > 0;

        public int Count(KawaiiPresetAdjustmentSyncStatus status)
        {
            return Entries.Count(entry => entry.Status == status);
        }

        public void Error(string code, string message, Object context = null)
        {
            Issues.Add(new KawaiiPresetAdjustmentSyncIssue
            {
                Severity = KawaiiMigrationSeverity.Error,
                Code = code,
                Message = message,
                Context = context
            });
        }

        public void Warning(string code, string message, Object context = null)
        {
            Issues.Add(new KawaiiPresetAdjustmentSyncIssue
            {
                Severity = KawaiiMigrationSeverity.Warning,
                Code = code,
                Message = message,
                Context = context
            });
        }
    }

    internal sealed class KawaiiPresetAdjustmentExecutionResult
    {
        public KawaiiPresetAdjustmentExecutionStatus Status;
        public KawaiiPresetAdjustmentSyncPlan Plan;
        public int AppliedSetCount;
        public int AppliedClearCount;
        public string Message = "";
    }

    internal sealed class KawaiiPresetAdjustmentSyncExecutor
    {
        internal const string TargetInvalidCode = "PT-KAS005";
        internal const string SourceMotionMissingCode = "PT-KAS006";
        internal const string SourceMotionAmbiguousCode = "PT-KAS007";
        internal const string TargetMotionMissingCode = "PT-KAS008";
        internal const string TargetMotionAmbiguousCode = "PT-KAS009";
        internal const string TargetIncompatibleCode = "PT-KAS010";
        internal const string StalePlanCode = "PT-KAS011";
        internal const string RollbackCode = "PT-KAS012";
        private const string UndoName = "KawaiiPosing 調整クリップを同期";

        private readonly Func<PoseTuneRoot, ValidationReport> validator;

        internal KawaiiPresetAdjustmentSyncExecutor(Func<PoseTuneRoot, ValidationReport> validator = null)
        {
            this.validator = validator ?? (root =>
                new PoseValidator().Validate(new PoseGraphCollector().Collect(root)));
        }

        public KawaiiPresetAdjustmentSyncPlan CreatePlan(PoseTuneRoot root, Preset preset)
        {
            var plan = new KawaiiPresetAdjustmentSyncPlan
            {
                Root = root,
                Preset = preset
            };

            if (!ValidateTarget(root, plan))
            {
                plan.Fingerprint = BuildFingerprint(plan);
                return plan;
            }

            if (!KawaiiPresetSerializedReader.TryRead(
                    preset,
                    out var dto,
                    out var errorCode,
                    out var errorMessage))
            {
                plan.Error(errorCode, errorMessage, preset);
                plan.Fingerprint = BuildFingerprint(plan);
                return plan;
            }

            foreach (var warning in dto.Warnings)
            {
                plan.Warning(warning.Code, warning.Message, warning.Context ?? preset);
            }

            BuildEntries(plan, dto);
            plan.Fingerprint = BuildFingerprint(plan);
            return plan;
        }

        public KawaiiPresetAdjustmentExecutionResult Execute(KawaiiPresetAdjustmentSyncPlan expectedPlan)
        {
            if (expectedPlan == null)
            {
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.Invalid,
                    Plan = new KawaiiPresetAdjustmentSyncPlan(),
                    Message = "先にドライランを実行してください。"
                };
            }

            var currentPlan = CreatePlan(expectedPlan.Root, expectedPlan.Preset);
            if (!string.Equals(expectedPlan.Fingerprint, currentPlan.Fingerprint, StringComparison.Ordinal))
            {
                currentPlan.Warning(StalePlanCode, "入力またはPoseTuneの状態が変わりました。最新のドライラン結果を確認してください。", currentPlan.Root);
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.Stale,
                    Plan = currentPlan,
                    Message = "状態変更を検出したため同期しませんでした。"
                };
            }

            if (currentPlan.HasErrors)
            {
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.Invalid,
                    Plan = currentPlan,
                    Message = "ドライランのErrorを解消してください。"
                };
            }

            if (!currentPlan.HasChanges)
            {
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.NoChanges,
                    Plan = currentPlan,
                    Message = "同期が必要な調整クリップはありません。"
                };
            }

            ValidationReport baseline;
            try
            {
                baseline = validator(currentPlan.Root) ?? new ValidationReport();
            }
            catch (Exception exception)
            {
                currentPlan.Error(RollbackCode, "同期前Validationに失敗しました: " + exception.Message, currentPlan.Root);
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.Invalid,
                    Plan = currentPlan,
                    Message = "同期前Validationに失敗したため変更しませんでした。"
                };
            }

            var changes = currentPlan.Entries
                .Where(entry => entry.Status is KawaiiPresetAdjustmentSyncStatus.Set or KawaiiPresetAdjustmentSyncStatus.Clear)
                .ToArray();
            var setCount = changes.Count(entry => entry.Status == KawaiiPresetAdjustmentSyncStatus.Set);
            var clearCount = changes.Length - setCount;

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                var targets = changes.Select(entry => entry.Target).Distinct().Cast<Object>().ToArray();
                Undo.RecordObjects(targets, UndoName);
                foreach (var entry in changes)
                {
                    entry.Target.adjustmentClip = entry.SourceAdjustmentClip;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(entry.Target);
                    EditorUtility.SetDirty(entry.Target);
                }

                var committed = validator(currentPlan.Root) ?? new ValidationReport();
                var newErrors = NewValidationErrors(baseline, committed).ToArray();
                if (newErrors.Length > 0)
                {
                    throw new KawaiiPresetAdjustmentSyncAbortException(
                        "同期後に新しいValidation Errorが発生しました: " +
                        string.Join(", ", newErrors.Select(error => error.Code).Distinct()));
                }

                Undo.CollapseUndoOperations(undoGroup);
                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.Applied,
                    Plan = CreatePlan(currentPlan.Root, currentPlan.Preset),
                    AppliedSetCount = setCount,
                    AppliedClearCount = clearCount,
                    Message = $"調整クリップを同期しました。設定={setCount}, クリア={clearCount}"
                };
            }
            catch (Exception exception)
            {
                try
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                }
                catch (Exception rollbackException)
                {
                    Debug.LogException(rollbackException, currentPlan.Root);
                }

                var rolledBackPlan = CreatePlan(currentPlan.Root, currentPlan.Preset);
                rolledBackPlan.Error(RollbackCode, "同期をロールバックしました: " + exception.Message, currentPlan.Root);
                if (exception is not KawaiiPresetAdjustmentSyncAbortException)
                {
                    Debug.LogException(exception, currentPlan.Root);
                }

                return new KawaiiPresetAdjustmentExecutionResult
                {
                    Status = KawaiiPresetAdjustmentExecutionStatus.RolledBack,
                    Plan = rolledBackPlan,
                    Message = "同期に失敗したため変更をロールバックしました。"
                };
            }
        }

        private static bool ValidateTarget(PoseTuneRoot root, KawaiiPresetAdjustmentSyncPlan plan)
        {
            if (root == null)
            {
                plan.Error(TargetInvalidCode, "対象 PoseTuneRoot を選択してください。");
                return false;
            }

            if (EditorUtility.IsPersistent(root) || PrefabUtility.IsPartOfImmutablePrefab(root))
            {
                plan.Error(TargetInvalidCode, "対象 PoseTuneRoot は現在のSceneまたはPrefab Stageで編集できません。", root);
                return false;
            }

            return true;
        }

        private static void BuildEntries(KawaiiPresetAdjustmentSyncPlan plan, KawaiiPosingSystemDto dto)
        {
            var sources = new List<SourcePose>();
            foreach (var layer in dto.Layers.OrderBy(layer => layer.Index))
            {
                foreach (var animation in layer.Animations.OrderBy(animation => animation.Index))
                {
                    sources.Add(new SourcePose
                    {
                        Key = $"define:{layer.Index}:{animation.Index}",
                        DisplayName = DisplayName(layer, animation),
                        Motion = animation.Motion,
                        AdjustmentClip = animation.AdjustmentClip
                    });
                }
            }

            foreach (var sourceOverride in dto.Overrides.OrderBy(sourceOverride => sourceOverride.Index))
            {
                sources.Add(new SourcePose
                {
                    Key = $"override:{sourceOverride.Index}",
                    DisplayName = "Override/" +
                                      (string.IsNullOrWhiteSpace(sourceOverride.StateType)
                                          ? sourceOverride.Index.ToString()
                                          : sourceOverride.StateType),
                    Motion = sourceOverride.Motion,
                    AdjustmentClip = sourceOverride.AdjustmentClip
                });
            }

            var targets = plan.Root.GetComponentsInChildren<PoseClip>(true)
                .Where(pose => pose != null && pose.GetComponentInParent<PoseTuneRoot>(true) == plan.Root)
                .ToArray();
            var sourceCounts = sources
                .Where(source => source.Motion != null)
                .GroupBy(source => source.Motion)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (var source in sources)
            {
                var entry = new KawaiiPresetAdjustmentSyncEntry
                {
                    SourceKey = source.Key,
                    DisplayName = source.DisplayName,
                    SourceMotion = source.Motion,
                    SourceAdjustmentClip = source.AdjustmentClip,
                    Context = plan.Preset
                };
                plan.Entries.Add(entry);

                if (source.Motion == null)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Unmatched;
                    entry.Message = "PresetのベースMotionが未設定です。";
                    plan.Warning(SourceMotionMissingCode, entry.DisplayName + ": " + entry.Message, plan.Preset);
                    continue;
                }

                if (sourceCounts[source.Motion] != 1)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Ambiguous;
                    entry.Message = "Preset内で同じベースMotionが複数回使われています。";
                    plan.Warning(SourceMotionAmbiguousCode, entry.DisplayName + ": " + entry.Message, source.Motion);
                    continue;
                }

                var matchingTargets = targets.Where(pose => pose.sourceMotion == source.Motion).ToArray();
                entry.CandidateTargets = matchingTargets;
                if (matchingTargets.Length == 0)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Unmatched;
                    entry.Message = "sourceMotionが一致するPoseClipがありません。";
                    plan.Warning(TargetMotionMissingCode, entry.DisplayName + ": " + entry.Message, source.Motion);
                    continue;
                }

                if (matchingTargets.Length != 1)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Ambiguous;
                    entry.Message = "sourceMotionが一致するPoseClipが複数あります。";
                    plan.Warning(TargetMotionAmbiguousCode, entry.DisplayName + ": " + entry.Message, source.Motion);
                    continue;
                }

                entry.Target = matchingTargets[0];
                entry.Context = entry.Target;
                if (entry.Target.compatibilityProfile != PoseSourceCompatibilityProfile.KawaiiPosing ||
                    entry.Target.adjustmentApplyMode != PoseAdjustmentApplyMode.AdditiveKawaiiCompatible)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Incompatible;
                    entry.Message = "Kawaii互換profile/apply modeではないため変更しません。";
                    plan.Warning(TargetIncompatibleCode, entry.DisplayName + ": " + entry.Message, entry.Target);
                    continue;
                }

                if (entry.Target.adjustmentClip == source.AdjustmentClip)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.NoChange;
                    entry.Message = "調整クリップは同期済みです。";
                }
                else if (source.AdjustmentClip == null)
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Clear;
                    entry.Message = "既存の調整クリップをクリアします。";
                }
                else
                {
                    entry.Status = KawaiiPresetAdjustmentSyncStatus.Set;
                    entry.Message = "Presetの調整クリップを設定します。";
                }
            }
        }

        private static string DisplayName(KawaiiLayerDto layer, KawaiiAnimationDto animation)
        {
            var layerName = string.IsNullOrWhiteSpace(layer.MenuName) ? "Layer " + layer.Index : layer.MenuName;
            var poseName = string.IsNullOrWhiteSpace(animation.DisplayName)
                ? animation.Motion != null ? animation.Motion.name : "Pose " + animation.Index
                : animation.DisplayName;
            return layerName + "/" + poseName;
        }

        private static string BuildFingerprint(KawaiiPresetAdjustmentSyncPlan plan)
        {
            var builder = new StringBuilder();
            AppendObjectIdentity(builder, plan.Root);
            AppendObjectIdentity(builder, plan.Preset);
            if (plan.Preset != null)
            {
                var path = AssetDatabase.GetAssetPath(plan.Preset);
                if (!string.IsNullOrEmpty(path))
                {
                    builder.Append('|').Append(AssetDatabase.GetAssetDependencyHash(path));
                }
            }

            if (plan.Root != null)
            {
                foreach (var pose in plan.Root.GetComponentsInChildren<PoseClip>(true)
                             .Where(pose => pose != null && pose.GetComponentInParent<PoseTuneRoot>(true) == plan.Root)
                             .OrderBy(pose => pose.GetInstanceID()))
                {
                    builder.Append("|pose:").Append(pose.GetInstanceID());
                    AppendObjectIdentity(builder, pose.sourceMotion);
                    AppendObjectIdentity(builder, pose.adjustmentClip);
                    builder.Append(':').Append((int)pose.compatibilityProfile)
                        .Append(':').Append((int)pose.adjustmentApplyMode);
                }
            }

            foreach (var entry in plan.Entries.OrderBy(entry => entry.SourceKey, StringComparer.Ordinal))
            {
                builder.Append("|entry:").Append(entry.SourceKey).Append(':').Append((int)entry.Status);
                AppendObjectIdentity(builder, entry.SourceMotion);
                AppendObjectIdentity(builder, entry.SourceAdjustmentClip);
                AppendObjectIdentity(builder, entry.Target);
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendObjectIdentity(StringBuilder builder, Object value)
        {
            if (value == null)
            {
                builder.Append("|null");
                return;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId))
            {
                builder.Append('|').Append(guid).Append(':').Append(localId);
                return;
            }

            builder.Append("|instance:").Append(value.GetInstanceID());
        }

        private static IEnumerable<ValidationIssue> NewValidationErrors(
            ValidationReport baseline,
            ValidationReport committed)
        {
            var baselineKeys = new HashSet<string>(baseline.Errors.Select(ValidationKey), StringComparer.Ordinal);
            return committed.Errors.Where(issue => !baselineKeys.Contains(ValidationKey(issue)));
        }

        private static string ValidationKey(ValidationIssue issue)
        {
            return issue.Code + "\n" + issue.Message + "\n" +
                   (issue.Context != null ? issue.Context.GetInstanceID().ToString() : "null");
        }

        private sealed class SourcePose
        {
            public string Key = "";
            public string DisplayName = "";
            public Motion Motion;
            public AnimationClip AdjustmentClip;
        }

        private sealed class KawaiiPresetAdjustmentSyncAbortException : Exception
        {
            public KawaiiPresetAdjustmentSyncAbortException(string message) : base(message)
            {
            }
        }
    }
}
