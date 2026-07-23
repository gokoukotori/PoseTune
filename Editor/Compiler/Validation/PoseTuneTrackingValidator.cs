using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneTrackingValidator
    {
        public static void ValidateRootPolicies(PoseGraph graph, ValidationReport report)
        {
            if (graph.RootTrackingPolicyCount > 1)
            {
                report.Error(PoseTuneDiagnostics.DuplicateRootTrackingPolicies.Code,
                    "root 直下に PoseTrackingPolicy が複数あります。所有者ごとに1つだけ残してください。",
                    graph.RootComponent);
            }

            foreach (var group in graph.Groups.Where(group => group.Source != null))
            {
                ValidateDuplicateOwner(group.Source, report, "PoseGroup");
            }

            foreach (var pose in graph.Poses.Where(pose => pose.Source != null))
            {
                ValidateDuplicateOwner(pose.Source, report, "PoseClip");
                if (pose.Source.GetComponent<PoseTrackingPolicy>() == null &&
                    TrackingPolicyUtility.WasCustomizedFromPoseDefault(pose.Source.tracking))
                {
                    report.Warning(PoseTuneDiagnostics.LegacyInlineTrackingPolicy.Code,
                        "旧形式の PoseClip.tracking を読み取り互換で使用しています。PoseTrackingPolicy component へ変換してください。",
                        pose.Source);
                }
            }
        }

        public static void ValidatePose(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            // generateResetOnExit=false is a supported sticky-policy contract. It must not
            // be reported as an FBT warning merely because no reset request is generated.
        }

        private static void ValidateDuplicateOwner(Component owner, ValidationReport report, string ownerName)
        {
            if (owner.GetComponents<PoseTrackingPolicy>().Length <= 1)
            {
                return;
            }

            report.Error(PoseTuneDiagnostics.DuplicateRootTrackingPolicies.Code,
                $"{ownerName} に PoseTrackingPolicy が複数あります。所有者ごとに1つだけ残してください。",
                owner);
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
