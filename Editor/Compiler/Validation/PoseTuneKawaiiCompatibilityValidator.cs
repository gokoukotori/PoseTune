using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneKawaiiCompatibilityValidator
    {
        public static void ValidateGraph(PoseGraph graph, ValidationReport report)
        {
            ValidateSourceObjects(graph, report);
            ValidateHeightProfile(graph, report);
        }

        public static void ValidatePose(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            if (pose.CompatibilityProfile != PoseSourceCompatibilityProfile.KawaiiPosing)
            {
                return;
            }

            if (pose.Source != null && pose.Source.sourceMotion == null && pose.Source.clip != null)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiSourceMotionMissing.Code, "Kawaii profile の PoseClip に sourceMotion が未設定です。clip を sourceMotion に設定できます。", pose.Source);
            }

            if (pose.SourceMotion == null && pose.Clip == null)
            {
                report.Error(PoseTuneDiagnostics.KawaiiSourceMotionAbsent.Code, "Kawaii profile の PoseClip に source motion がありません。", pose.Source);
            }

            if (pose.SourceMotion is BlendTree && pose.Clip == null)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiBlendTreePreviewClipMissing.Code, "BlendTree source motion は保持されます。preview 用 clip がない場合は thumbnail 生成に制限があります。", pose.Source);
            }

            if (pose.RecenterRootXZToHead)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiRootRecenterApproximation.Code, "root recenter は first RootT key 基準の近似です。Kawaii の Head XZ sampling とは完全一致しません。", pose.Source);
                var animator = graph.AvatarRoot != null ? graph.AvatarRoot.GetComponent<Animator>() : null;
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                {
                    report.Warning(PoseTuneDiagnostics.KawaiiRootRecenterRequiresHumanoidVerification.Code, "root recenter は Humanoid avatar で runtime 確認が必要です。", pose.Source);
                }
            }

            if (!Mathf.Approximately(pose.RootYawOffsetDegrees, 0f) && pose.SourceMotion == null)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiRootYawOffsetSourceMotionMissing.Code, "root yaw offset を適用する source motion がありません。", pose.Source);
            }

            if (!Mathf.Approximately(pose.HumanoidOrientationOffsetYDegrees, 0f) && pose.SourceMotion == null)
            {
                report.Warning(PoseTuneDiagnostics.KawaiiHumanoidOrientationOffsetSourceMotionMissing.Code, "Humanoid orientation offset を適用する source motion がありません。", pose.Source);
            }

            if (pose.AdjustmentApplyMode == PoseAdjustmentApplyMode.AdditiveKawaiiCompatible &&
                pose.AdjustmentClip != null &&
                AnimationUtility.GetObjectReferenceCurveBindings(pose.AdjustmentClip).Any())
            {
                report.Warning(PoseTuneDiagnostics.KawaiiAdditiveAdjustmentObjectCurveFallback.Code, "Kawaii additive adjustment に object reference curve が含まれるため置換として扱われます。", pose.Source);
            }

            if (pose.AdjustmentApplyMode == PoseAdjustmentApplyMode.AdditiveKawaiiCompatible &&
                pose.AdjustmentClip != null &&
                AnimationUtility.GetCurveBindings(pose.AdjustmentClip).Any(IsHumanoidMuscleCurve))
            {
                report.Warning(PoseTuneDiagnostics.KawaiiAdditiveAdjustmentHumanoidCurveRequiresRebake.Code, "AdjustmentClip に humanoid muscle curve が含まれます。完全互換には rebake が必要です。", pose.Source);
            }
        }

        private static void ValidateSourceObjects(PoseGraph graph, ValidationReport report)
        {
            if (graph.AvatarRoot == null)
            {
                return;
            }

            foreach (var handle in KawaiiPosingDetector.FindSystems(graph.AvatarRoot))
            {
                var component = handle.Component;
                if (component == null ||
                    component.gameObject == null ||
                    !component.gameObject.activeInHierarchy ||
                    component.gameObject.CompareTag("EditorOnly"))
                {
                    continue;
                }

                report.Warning(
                    PoseTuneDiagnostics.KawaiiActiveSourceSystemRisk.Code,
                    "High risk: active な KawaiiPosing / PosingSystem が残っています。PoseTune と同時に build すると二重適用される可能性があります。EditorOnly 化または無効化してください。",
                    component);
            }
        }

        private static void ValidateHeightProfile(PoseGraph graph, ValidationReport report)
        {
            var height = graph.HeightAdjust;
            if (height == null ||
                height.blendProfile != HeightBlendProfile.KawaiiPosing ||
                height.applyMode == HeightApplyMode.HumanoidLevelOffset)
            {
                return;
            }

            report.Warning(PoseTuneDiagnostics.KawaiiHeightProfileApproximation.Code, "Kawaii height profile で HumanoidLevelOffset 以外の applyMode を使用しています。近似互換として扱われます。", height);
        }

        private static bool IsHumanoidMuscleCurve(EditorCurveBinding binding)
        {
            return binding.type == typeof(Animator) &&
                   !string.IsNullOrWhiteSpace(binding.propertyName) &&
                   binding.propertyName.StartsWith("Muscle.", System.StringComparison.Ordinal);
        }
    }
}
