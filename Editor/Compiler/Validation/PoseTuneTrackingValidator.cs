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

            foreach (var policy in PoseTuneTrackingPolicyResolver.UnsupportedPolicies(graph.RootComponent))
            {
                report.Error(PoseTuneDiagnostics.UnsupportedTrackingPolicyOwner.Code,
                    "PoseTrackingPolicy の owner は PoseTuneRoot 直下または PoseGroup と同じ GameObject に限定されています。" +
                    "編集可能な Scene instance または Prefab Stage で移動してください。",
                    policy);
            }
        }

        public static void ValidatePose(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            // generateResetOnExit=false is a supported sticky-policy contract. It must not
            // be reported as an FBT warning merely because no reset request is generated.
        }

        private static void ValidateDuplicateOwner(Component owner, ValidationReport report, string ownerName)
        {
            if (owner.GetComponents<PoseTrackingPolicy>()
                    .Count(PoseTuneAuthoringInclusion.ComponentEnabled) <= 1)
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
            foreach (var group in graph.Groups.Where(group => group.HasFullBodyTrackingOverride))
            {
                if (!graph.RootComponent.advancedSettings.allowFullBodyTracking)
                {
                    report.Warning(PoseTuneDiagnostics.FbtOverrideRequiresCompatibilityMode.Code, "FBT override がありますが、FBT 互換モードが無効なため FBT 用 state は生成されません。", group.Source);
                }

                var policy = group.FullBodyTrackingPolicy ?? TrackingPolicyData.DefaultForPose();
                if (policy.hip == TrackingMode.Animation ||
                    policy.leftFoot == TrackingMode.Animation ||
                    policy.rightFoot == TrackingMode.Animation)
                {
                    report.Warning(PoseTuneDiagnostics.FbtOverrideLowerBodyAnimationRisk.Code, "FBT override で Hip/Feet を Animation にするため、VRChat runtime / FBT 実機確認が必要です。", group.Source);
                }
            }
        }
    }
}
