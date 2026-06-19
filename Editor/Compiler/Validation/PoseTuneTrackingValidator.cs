using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneTrackingValidator
    {
        public static void ValidateRootPolicies(PoseGraph graph, ValidationReport report)
        {
            if (graph.RootTrackingPolicyCount > 1)
            {
                report.Warning(PoseTuneDiagnostics.DuplicateRootTrackingPolicies.Code, "root 直下に PoseTrackingPolicy が複数あります。最初の policy だけを fallback として使用します。", graph.RootComponent);
            }
        }

        public static void ValidatePose(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            if (!pose.GenerateResetOnExit && graph.RootComponent.disableWhenFullBodyTracking)
            {
                report.Warning(PoseTuneDiagnostics.TrackingResetDisabledForFbt.Code, "tracking reset が無効なため FBT 復帰挙動を確認してください。", pose.Source);
            }
        }

        public static void ValidateFbtCompatibility(PoseGraph graph, ValidationReport report)
        {
            ValidateFbtOverrides(graph, report);

            if (graph.RootComponent.disableWhenFullBodyTracking &&
                !graph.RootComponent.advancedSettings.allowFullBodyTracking)
            {
                report.Warning(PoseTuneDiagnostics.FullBodyTrackingDisabled.Code, "生成される遷移では 2 < TrackingType < 4 の条件で FBT を抑止します。", graph.RootComponent);
            }
        }

        private static void ValidateFbtOverrides(PoseGraph graph, ValidationReport report)
        {
            foreach (var pose in graph.Poses.Where(pose => pose.HasFullBodyTrackingOverride))
            {
                if (!graph.RootComponent.advancedSettings.allowFullBodyTracking)
                {
                    report.Warning(PoseTuneDiagnostics.FbtOverrideRequiresCompatibilityMode.Code, "FBT override がありますが、FBT 互換モードが無効なため FBT 用 state は生成されません。", pose.Source);
                }

                var policy = pose.FullBodyTrackingPolicy ?? TrackingPolicyData.DefaultForPose();
                if (policy.hip == TrackingMode.Animation ||
                    policy.leftFoot == TrackingMode.Animation ||
                    policy.rightFoot == TrackingMode.Animation)
                {
                    report.Warning(PoseTuneDiagnostics.FbtOverrideLowerBodyAnimationRisk.Code, "FBT override で Hip/Feet を Animation にするため、VRChat runtime / FBT 実機確認が必要です。", pose.Source);
                }
            }
        }
    }
}
