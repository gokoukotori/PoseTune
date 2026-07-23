using System;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiMigrationOptions
    {
        public bool createNewPoseTuneRoot = true;
        public PoseTuneRoot existingRoot;

        public bool preserveSourceParameterNames = true;
        public bool preserveExplicitMenuValues = true;
        public bool preserveInitialPose = true;
        public bool preserveCustomIcons = true;
        public bool preserveDisabledPosesAsDisabled;

        public KawaiiFootHeightMode footHeightMode = KawaiiFootHeightMode.StrictHumanoidLevel;
        public KawaiiBlendTreeMode blendTreeMode = KawaiiBlendTreeMode.PreserveMotion;
        public KawaiiRootRecenterMode rootRecenterMode = KawaiiRootRecenterMode.FirstRootKeyApproximation;
        public KawaiiRotationMode rotationMode = KawaiiRotationMode.HumanoidOrientationOffsetY;
        public KawaiiAdjustmentMode adjustmentMode = KawaiiAdjustmentMode.AdditiveCompatible;
        public KawaiiMotionTimeMode motionTimeMode = KawaiiMotionTimeMode.AnimatorOnlyParameter;
        public KawaiiOverrideImportMode overrideImportMode = KawaiiOverrideImportMode.ReportOnly;
        public KawaiiPoseSpaceMode poseSpaceMode = KawaiiPoseSpaceMode.DesktopOnlyCompatible;
        public KawaiiTargetLayerMode targetLayerMode = KawaiiTargetLayerMode.BaseStrict;
        public PoseSelectionSyncMode selectionSyncMode = PoseSelectionSyncMode.DirectGroupParameter;

        public bool addTrackingPolicy = true;
        public bool enableAutoContextSwitch = true;
        public bool disableWhenFullBodyTracking = true;

        public KawaiiSourceDisposition sourceDisposition = KawaiiSourceDisposition.KeepUnchanged;
        public bool confirmSharedSourceObjectMutation;
        public bool dryRunOnly;

        public static KawaiiMigrationOptions Default()
        {
            return new KawaiiMigrationOptions();
        }
    }

    internal enum KawaiiSourceDisposition
    {
        KeepUnchanged,
        MarkGameObjectEditorOnly,
        DeactivateGameObject
    }

    internal enum KawaiiFootHeightMode
    {
        Off,
        ApproximatePoseTuneRootOffset,
        StrictHumanoidLevel
    }

    internal enum KawaiiBlendTreeMode
    {
        Skip,
        FlattenLeaves,
        PreserveMotion
    }

    internal enum KawaiiRootRecenterMode
    {
        Off,
        BakeAtMigration,
        FirstRootKeyApproximation
    }

    internal enum KawaiiRotationMode
    {
        Off,
        BakeAtMigration,
        HumanoidOrientationOffsetY
    }

    internal enum KawaiiAdjustmentMode
    {
        PoseTuneReplaceCurves,
        AdditiveCompatible
    }

    internal enum KawaiiMotionTimeMode
    {
        Skip,
        AnimatorOnlyParameter,
        CustomFloatParameterWithRadialMenu
    }

    internal enum KawaiiOverrideImportMode
    {
        ReportOnly,
        ImportSupportedOnly,
        ImportAllAsCustomDisabled
    }

    internal enum KawaiiPoseSpaceMode
    {
        Off,
        PoseTuneDefault,
        DesktopOnlyCompatible
    }

    internal enum KawaiiTargetLayerMode
    {
        BaseStrict,
        ActionApproximate
    }

    internal static class KawaiiMigrationOptionSupport
    {
        public static bool IsSelectable(Enum value)
        {
            return value switch
            {
                _ => true
            };
        }

        public static string DisplayName(Enum value)
        {
            return value switch
            {
                KawaiiFootHeightMode.Off => "無効",
                KawaiiFootHeightMode.ApproximatePoseTuneRootOffset => "PoseTuneRoot オフセット近似",
                KawaiiFootHeightMode.StrictHumanoidLevel => "Humanoid Level 厳密",
                KawaiiBlendTreeMode.Skip => "スキップ",
                KawaiiBlendTreeMode.FlattenLeaves => "Leaf を展開",
                KawaiiBlendTreeMode.PreserveMotion => "Motion を保持",
                KawaiiRootRecenterMode.Off => "無効",
                KawaiiRootRecenterMode.BakeAtMigration => "移行時に焼き込み",
                KawaiiRootRecenterMode.FirstRootKeyApproximation => "First Root Key 近似",
                KawaiiRotationMode.Off => "無効",
                KawaiiRotationMode.BakeAtMigration => "移行時に焼き込み",
                KawaiiRotationMode.HumanoidOrientationOffsetY => "Humanoid Orientation Offset Y",
                KawaiiAdjustmentMode.PoseTuneReplaceCurves => "PoseTune カーブ置換",
                KawaiiAdjustmentMode.AdditiveCompatible => "加算互換",
                KawaiiMotionTimeMode.Skip => "スキップ",
                KawaiiMotionTimeMode.AnimatorOnlyParameter => "Animator 専用パラメータ",
                KawaiiMotionTimeMode.CustomFloatParameterWithRadialMenu => "カスタム Float + Radial メニュー",
                KawaiiOverrideImportMode.ReportOnly => "レポートのみ",
                KawaiiOverrideImportMode.ImportSupportedOnly => "対応項目のみ取り込み",
                KawaiiOverrideImportMode.ImportAllAsCustomDisabled => "すべてカスタム無効として取り込み",
                KawaiiPoseSpaceMode.Off => "無効",
                KawaiiPoseSpaceMode.PoseTuneDefault => "PoseTune 標準",
                KawaiiPoseSpaceMode.DesktopOnlyCompatible => "デスクトップのみ互換",
                KawaiiTargetLayerMode.BaseStrict => "Base レイヤー厳密",
                KawaiiTargetLayerMode.ActionApproximate => "Action レイヤー近似",
                KawaiiSourceDisposition.KeepUnchanged => "移行元を変更しない",
                KawaiiSourceDisposition.MarkGameObjectEditorOnly => "移行元 GameObject を EditorOnly",
                KawaiiSourceDisposition.DeactivateGameObject => "移行元 GameObject を無効化",
                PoseSelectionSyncMode.DirectGroupParameter => "グループパラメータを直接同期",
                PoseSelectionSyncMode.CompressedPoseId => "圧縮 Pose ID",
                _ => value.ToString()
            };
        }

        public static string DisplaySummary(KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            var pairs = new[]
            {
                $"足の高さ={DisplayName(options.footHeightMode)}",
                $"BlendTree 互換={DisplayName(options.blendTreeMode)}",
                $"Root 再中心化={DisplayName(options.rootRecenterMode)}",
                $"回転={DisplayName(options.rotationMode)}",
                $"調整={DisplayName(options.adjustmentMode)}",
                $"MotionTime={DisplayName(options.motionTimeMode)}",
                $"OverrideDefines 取り込み={DisplayName(options.overrideImportMode)}",
                $"PoseSpace 互換={DisplayName(options.poseSpaceMode)}",
                $"対象レイヤー={DisplayName(options.targetLayerMode)}",
                $"同期方式={DisplayName(options.selectionSyncMode)}",
                $"自動コンテキスト={DisplayBool(options.enableAutoContextSwitch)}",
                $"移行元={DisplayName(options.sourceDisposition)}"
            };
            return string.Join(", ", pairs.Where(pair => !string.IsNullOrWhiteSpace(pair)));
        }

        public static string Summary(KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            var pairs = new[]
            {
                $"footHeightMode={options.footHeightMode}",
                $"blendTreeMode={options.blendTreeMode}",
                $"rootRecenterMode={options.rootRecenterMode}",
                $"rotationMode={options.rotationMode}",
                $"adjustmentMode={options.adjustmentMode}",
                $"motionTimeMode={options.motionTimeMode}",
                $"overrideImportMode={options.overrideImportMode}",
                $"poseSpaceMode={options.poseSpaceMode}",
                $"targetLayerMode={options.targetLayerMode}",
                $"selectionSyncMode={options.selectionSyncMode}",
                $"autoContext={options.enableAutoContextSwitch}",
                $"sourceDisposition={options.sourceDisposition}"
            };
            return string.Join(", ", pairs.Where(pair => !string.IsNullOrWhiteSpace(pair)));
        }

        private static string DisplayBool(bool value)
        {
            return value ? "有効" : "無効";
        }
    }
}
