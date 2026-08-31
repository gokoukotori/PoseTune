using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneDiagnostics
    {
        public static readonly PoseTuneDiagnosticDescriptor RootMissing = new("PT-R000");
        public static readonly PoseTuneDiagnosticDescriptor RootOutsideAvatarDescriptor = new("PT-R001");
        public static readonly PoseTuneDiagnosticDescriptor AvatarAnimatorMissing = new("PT-R002");
        public static readonly PoseTuneDiagnosticDescriptor AvatarAnimatorAvatarMissing = new("PT-R003");
        public static readonly PoseTuneDiagnosticDescriptor AvatarAnimatorNonHumanoid = new("PT-R004");
        public static readonly PoseTuneDiagnosticDescriptor MultipleRootComponents = new("PT-R005");
        public static readonly PoseTuneDiagnosticDescriptor RootNamespaceTooLong = new(
            "PT-R006",
            "PoseTuneRoot の Parameter Namespace を短くしてください。");
        public static readonly PoseTuneDiagnosticDescriptor GroupHasNoPose = new(
            "PT-G001",
            "PoseClip を追加するか、不要な PoseGroup を削除してください。");
        public static readonly PoseTuneDiagnosticDescriptor GroupHasNoEnabledPose = new(
            "PT-G003",
            "PoseClip を有効化するか、不要な PoseGroup を削除してください。");
        public static readonly PoseTuneDiagnosticDescriptor GroupSyncedParameterBudgetExceeded = new(
            "PT-G004",
            "同期する PoseGroup を減らすか、一部の PoseGroup をローカル専用にしてください。");
        public static readonly PoseTuneDiagnosticDescriptor GroupParameterConflict = new("PT-G002");
        public static readonly PoseTuneDiagnosticDescriptor GroupGeneratedParameterConflict = new("PT-G005");
        public static readonly PoseTuneDiagnosticDescriptor GroupNonExclusiveOverridePose = new("PT-G006");
        public static readonly PoseTuneDiagnosticDescriptor ClipMotionMissing = new(
            "PT-C001",
            "PoseClip に AnimationClip または sourceMotion を設定してください。");
        public static readonly PoseTuneDiagnosticDescriptor ClipZeroLength = new(
            "PT-C002",
            "AnimationClip に curve を追加するか、Motion Time を無効にするか、不要な PoseClip を削除してください。");
        public static readonly PoseTuneDiagnosticDescriptor ClipRootTransformCurves = new(
            "PT-C003",
            "時間変化する root curve を削除し、静的な補正は PoseClip の root offset 設定へ移してください。");
        public static readonly PoseTuneDiagnosticDescriptor ClipUnsupportedCurves = new(
            "PT-C004",
            "警告を解消するには、元の clip から Transform / Animator / BlendShape 以外の curve を削除してください。");
        public static readonly PoseTuneDiagnosticDescriptor ClipMultipleInitial = new(
            "PT-C005",
            "同じ PoseGroup 内で Initial を ON にする PoseClip を 1 つだけにしてください。");
        public static readonly PoseTuneDiagnosticDescriptor ClipLoopMismatch = new(
            "PT-C007",
            "PoseClip の Loop と AnimationClip の Loop Time を同じ設定にしてください。");
        public static readonly PoseTuneDiagnosticDescriptor AdditivePoseOutputOffset = new("PT-C009");
        public static readonly PoseTuneDiagnosticDescriptor AutoPosePriorityAmbiguous = new("PT-C010");
        public static readonly PoseTuneDiagnosticDescriptor ClipConfigurationInvalid = new("PT-C006");
        public static readonly PoseTuneDiagnosticDescriptor MotionTimeInvalid = ClipConfigurationInvalid;
        public static readonly PoseTuneDiagnosticDescriptor ClipMenuValueInvalid = ClipConfigurationInvalid;
        public static readonly PoseTuneDiagnosticDescriptor MotionTimeGeneratedHeightConflict = new("PT-C008");
        public static readonly PoseTuneDiagnosticDescriptor HeightRuntimeAutoCorrectionRequiresVerification = new("PT-H001");
        public static readonly PoseTuneDiagnosticDescriptor HeightMaxAutoOffsetLarge = new("PT-H002");
        public static readonly PoseTuneDiagnosticDescriptor MultipleHeightAdjust = new("PT-H003");
        public static readonly PoseTuneDiagnosticDescriptor ParameterNameConflict = new("PT-P001");
        public static readonly PoseTuneDiagnosticDescriptor ParameterSyncedBudgetExceeded = new(
            "PT-P002",
            "同期パラメータを減らすか、不要な既存 Expression Parameter を整理してください。");
        public static readonly PoseTuneDiagnosticDescriptor ParameterNameEmpty = new("PT-P003");
        public static readonly PoseTuneDiagnosticDescriptor ParameterReservedName = new("PT-P004");
        public static readonly PoseTuneDiagnosticDescriptor ExistingExpressionParameterTypeConflict = new("PT-P005");
        public static readonly PoseTuneDiagnosticDescriptor ExistingAnimatorParameterTypeConflict = new("PT-P006");
        public static readonly PoseTuneDiagnosticDescriptor ExpressionParameterCountExceeded = new("PT-P007");
        public static readonly PoseTuneDiagnosticDescriptor ExpressionParameterCountNearLimit = new("PT-P008");
        public static readonly PoseTuneDiagnosticDescriptor ParameterConditionInvalid = new(
            "PT-P009",
            "条件 parameter の値の型と比較方法を対応する組み合わせに修正してください。");
        public static readonly PoseTuneDiagnosticDescriptor SharedPoseSelectionSummary = new("PT-P010");
        public static readonly PoseTuneDiagnosticDescriptor SharedPoseSelectionCapacityExceeded = new(
            "PT-P011",
            "共有バンクの PoseClip を255個以下に減らすか、Saved設定を分けてください。");
        public static readonly PoseTuneDiagnosticDescriptor SharedPoseSelectionInitialConflict = new(
            "PT-P012",
            "共有対象全体で初期ポーズを1つ以下にしてください。");
        public static readonly PoseTuneDiagnosticDescriptor SharedPoseSelectionMetadataConflict = new(
            "PT-P013",
            "既存Expression ParameterのSavedまたはSynced設定を共有Pose IDと一致させてください。");
        public static readonly PoseTuneDiagnosticDescriptor UnsupportedTargetLayer = new("PT-A001");
        public static readonly PoseTuneDiagnosticDescriptor AnimatorMissingResetExitTransition = new(
            "PT-A002",
            "生成された Animator の Pose variant から専用 cleanup/handoff への終了遷移を確認してください。");
        public static readonly PoseTuneDiagnosticDescriptor AnimatorTrackingResetStateMissing = new("PT-A003");
        public static readonly PoseTuneDiagnosticDescriptor AnimatorFbtPoseEntryRisk = new(
            "PT-A004",
            "FBT でこのポーズを無効にしたい場合は TrackingType 条件を追加してください。");
        public static readonly PoseTuneDiagnosticDescriptor ActionLayerWeightControlConflictRisk = new("PT-A005");
        public static readonly PoseTuneDiagnosticDescriptor ActionLayerWeightControlDisabled = new("PT-A006");
        public static readonly PoseTuneDiagnosticDescriptor BaseLayerLowerBodyPoseRisk = new("PT-A007");
        public static readonly PoseTuneDiagnosticDescriptor BuildGraphContextMissing = new("PT-B000");
        public static readonly PoseTuneDiagnosticDescriptor BuildGeneratedMarkerMissing = new(
            "PT-B001",
            "PoseTune のビルド出力が生成されているか確認し、必要なら再ビルドしてください。");
        public static readonly PoseTuneDiagnosticDescriptor BuildGeneratedVersionMissing = new("PT-B002");
        public static readonly PoseTuneDiagnosticDescriptor BuildGraphHashMissing = new("PT-B003");
        public static readonly PoseTuneDiagnosticDescriptor BuildGraphHashMismatch = new(
            "PT-B004",
            "PoseTune の生成出力が古い可能性があります。再ビルドしてください。");
        public static readonly PoseTuneDiagnosticDescriptor BuildExpressionParameterMissing = new("PT-B005");
        public static readonly PoseTuneDiagnosticDescriptor BuildMenuControlMissing = new("PT-B006");
        public static readonly PoseTuneDiagnosticDescriptor BuildPlayableLayerMissing = new("PT-B007");
        public static readonly PoseTuneDiagnosticDescriptor BuildGeneratedAnimatorAssetsSaveFailed = new("PT-B010");
        public static readonly PoseTuneDiagnosticDescriptor BuildExpressionParameterMetadataMismatch = new("PT-B011");
        public static readonly PoseTuneDiagnosticDescriptor MenuControlLimitExceeded = new("PT-M001");
        public static readonly PoseTuneDiagnosticDescriptor MotionTimeAnimatorStateMenuUnavailable = new("PT-M002");
        public static readonly PoseTuneDiagnosticDescriptor ManualControlMenuMissing = new("PT-M003");
        public static readonly PoseTuneDiagnosticDescriptor DuplicateRootTrackingPolicies = new("PT-T001");
        public static readonly PoseTuneDiagnosticDescriptor TrackingResetDisabledForFbt = new("PT-T002");
        public static readonly PoseTuneDiagnosticDescriptor UnsupportedTrackingPolicyOwner = new(
            "PT-T004",
            "PoseTrackingPolicy を PoseTuneRoot 直下または PoseGroup と同じ GameObject へ移してください。");
        public static readonly PoseTuneDiagnosticDescriptor FullBodyTrackingDisabled = new(
            "PT-FBT001",
            "FBT でも PoseTune を有効にしたい場合は PoseTuneRoot の Disable When Full Body Tracking を OFF にしてください。");
        public static readonly PoseTuneDiagnosticDescriptor FbtOverrideRequiresCompatibilityMode = new("PT-FBT002");
        public static readonly PoseTuneDiagnosticDescriptor FbtOverrideLowerBodyAnimationRisk = new("PT-FBT003");
        public static readonly PoseTuneDiagnosticDescriptor GoroneSystemExMissing = new("PT-GR001");
        public static readonly PoseTuneDiagnosticDescriptor ClassicSupineDetected = new("PT-GR002");
        public static readonly PoseTuneDiagnosticDescriptor MultipleGoroneSystemExCompatibility = new("PT-GR003");
        public static readonly PoseTuneDiagnosticDescriptor VrcSupineParameterTypeInvalid = new("PT-GR004");
        public static readonly PoseTuneDiagnosticDescriptor GoroneSystemExBaseLayerConflictRisk = new("PT-GR005");
        public static readonly PoseTuneDiagnosticDescriptor VrcSupineParameterMissing = new("PT-GR006");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiMigrationSummaryInfo = new("PT-K000");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiMigrationSourceMissing = new("PT-K001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSerializedReadWarning = new("PT-K002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiPoseSourceMotionMissing = new("PT-K003");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiDisabledPoseSkipped = new("PT-K004");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiThumbnailPackNotMigrated = new("PT-KI001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiMigrationOptionsInfo = new("PT-KOPT");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSourceMergeTrackingControlDisabled = new("PT-KT002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiControllerTrackingMergeNotStrictlyMigrated = new("PT-KT003");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiUnsupportedMigrationOption = new("PT-KU001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiActionLayerApproximation = new("PT-KL001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSyncedParameterDirectGroupApproximation = new("PT-KS002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiPoseThresholdApproximation = new("PT-KT001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiFootHeightParameterMetadataApplied = new("PT-KH002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiBlendTreePoseSkipped = new("PT-KB-SKIP");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiMotionTimeParameterMissing = new("PT-KM001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiOverrideUnsupported = new("PT-KO001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiOverrideTrackingNotStrictlyMigrated = new("PT-KO002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSourceMotionMissing = new("PT-KC001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSourceMotionAbsent = new("PT-KC002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiBlendTreeFlattenFallback = new("PT-KB-FALLBACK");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiRootRecenterRequiresHumanoidVerification = new("PT-KR001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiRootYawOffsetSourceMotionMissing = new("PT-KR002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiRootRecenterApproximation = new("PT-KR003");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiHumanoidOrientationOffsetSourceMotionMissing = new("PT-KR004");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiAdditiveAdjustmentObjectCurveFallback = new("PT-KA001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiAdditiveAdjustmentHumanoidCurveRequiresRebake = new("PT-KA002");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiSourceAutoImportAvatarAnimationsUnsupported =
            KawaiiAdditiveAdjustmentHumanoidCurveRequiresRebake;
        public static readonly PoseTuneDiagnosticDescriptor KawaiiActiveSourceSystemRisk = new("PT-KS001");
        public static readonly PoseTuneDiagnosticDescriptor KawaiiHeightProfileApproximation = new("PT-KH001");

        private static readonly PoseTuneDiagnosticDescriptor[] All =
        {
            RootMissing,
            RootOutsideAvatarDescriptor,
            AvatarAnimatorMissing,
            AvatarAnimatorAvatarMissing,
            AvatarAnimatorNonHumanoid,
            MultipleRootComponents,
            RootNamespaceTooLong,
            GroupHasNoPose,
            GroupHasNoEnabledPose,
            GroupSyncedParameterBudgetExceeded,
            GroupParameterConflict,
            GroupGeneratedParameterConflict,
            GroupNonExclusiveOverridePose,
            ClipMotionMissing,
            ClipZeroLength,
            ClipRootTransformCurves,
            ClipUnsupportedCurves,
            ClipMultipleInitial,
            ClipLoopMismatch,
            AdditivePoseOutputOffset,
            AutoPosePriorityAmbiguous,
            ClipConfigurationInvalid,
            MotionTimeGeneratedHeightConflict,
            HeightRuntimeAutoCorrectionRequiresVerification,
            HeightMaxAutoOffsetLarge,
            MultipleHeightAdjust,
            ParameterNameConflict,
            ParameterSyncedBudgetExceeded,
            ParameterNameEmpty,
            ParameterReservedName,
            ExistingExpressionParameterTypeConflict,
            ExistingAnimatorParameterTypeConflict,
            ExpressionParameterCountExceeded,
            ExpressionParameterCountNearLimit,
            ParameterConditionInvalid,
            SharedPoseSelectionSummary,
            SharedPoseSelectionCapacityExceeded,
            SharedPoseSelectionInitialConflict,
            SharedPoseSelectionMetadataConflict,
            UnsupportedTargetLayer,
            AnimatorMissingResetExitTransition,
            AnimatorTrackingResetStateMissing,
            AnimatorFbtPoseEntryRisk,
            ActionLayerWeightControlConflictRisk,
            ActionLayerWeightControlDisabled,
            BaseLayerLowerBodyPoseRisk,
            BuildGraphContextMissing,
            BuildGeneratedMarkerMissing,
            BuildGeneratedVersionMissing,
            BuildGraphHashMissing,
            BuildGraphHashMismatch,
            BuildExpressionParameterMissing,
            BuildMenuControlMissing,
            BuildPlayableLayerMissing,
            BuildGeneratedAnimatorAssetsSaveFailed,
            BuildExpressionParameterMetadataMismatch,
            MenuControlLimitExceeded,
            MotionTimeAnimatorStateMenuUnavailable,
            ManualControlMenuMissing,
            DuplicateRootTrackingPolicies,
            TrackingResetDisabledForFbt,
            UnsupportedTrackingPolicyOwner,
            FullBodyTrackingDisabled,
            FbtOverrideRequiresCompatibilityMode,
            FbtOverrideLowerBodyAnimationRisk,
            GoroneSystemExMissing,
            ClassicSupineDetected,
            MultipleGoroneSystemExCompatibility,
            VrcSupineParameterTypeInvalid,
            GoroneSystemExBaseLayerConflictRisk,
            VrcSupineParameterMissing,
            KawaiiMigrationSummaryInfo,
            KawaiiMigrationSourceMissing,
            KawaiiSerializedReadWarning,
            KawaiiPoseSourceMotionMissing,
            KawaiiDisabledPoseSkipped,
            KawaiiThumbnailPackNotMigrated,
            KawaiiMigrationOptionsInfo,
            KawaiiSourceMergeTrackingControlDisabled,
            KawaiiControllerTrackingMergeNotStrictlyMigrated,
            KawaiiUnsupportedMigrationOption,
            KawaiiActionLayerApproximation,
            KawaiiSyncedParameterDirectGroupApproximation,
            KawaiiPoseThresholdApproximation,
            KawaiiFootHeightParameterMetadataApplied,
            KawaiiBlendTreePoseSkipped,
            KawaiiMotionTimeParameterMissing,
            KawaiiOverrideUnsupported,
            KawaiiOverrideTrackingNotStrictlyMigrated,
            KawaiiSourceMotionMissing,
            KawaiiSourceMotionAbsent,
            KawaiiBlendTreeFlattenFallback,
            KawaiiRootRecenterRequiresHumanoidVerification,
            KawaiiRootYawOffsetSourceMotionMissing,
            KawaiiRootRecenterApproximation,
            KawaiiHumanoidOrientationOffsetSourceMotionMissing,
            KawaiiAdditiveAdjustmentObjectCurveFallback,
            KawaiiAdditiveAdjustmentHumanoidCurveRequiresRebake,
            KawaiiActiveSourceSystemRisk,
            KawaiiHeightProfileApproximation
        };
        private static readonly Dictionary<string, string> FixHints = All
            .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.FixHint))
            .ToDictionary(descriptor => descriptor.Code, descriptor => descriptor.FixHint);

        public static string FixHint(string code)
        {
            return !string.IsNullOrWhiteSpace(code) && FixHints.TryGetValue(code, out var hint)
                ? hint
                : "";
        }
    }

    internal sealed class PoseTuneDiagnosticDescriptor
    {
        public PoseTuneDiagnosticDescriptor(string code, string fixHint = "")
        {
            Code = code;
            FixHint = fixHint;
        }

        public string Code { get; }
        public string FixHint { get; }
    }
}
