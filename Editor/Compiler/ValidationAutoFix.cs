using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum AutoFixSafety
    {
        Safe,
        Reversible,
        Destructive,
        RequiresAssetWrite
    }

    internal interface IPoseTuneAutoFix
    {
        string Code { get; }
        string Label { get; }
        AutoFixSafety Safety { get; }
        bool IncludeInBatch { get; }
        bool CanFix(ValidationIssue issue, PoseGraph graph);
        void Apply(ValidationIssue issue, PoseGraph graph);
    }

    internal sealed class PoseTuneAutoFixRegistry
    {
        private readonly List<IPoseTuneAutoFix> fixes = new()
        {
            new DisableGroupAutoFix(PoseTuneDiagnostics.GroupHasNoPose.Code, "この group を build 対象外にする"),
            new DisableGroupAutoFix(PoseTuneDiagnostics.GroupHasNoEnabledPose.Code, "この group を build 対象外にする"),
            new ClearGroupParameterAutoFix(PoseTuneDiagnostics.GroupParameterConflict.Code, "この group の明示 parameter 名をクリア"),
            new ClearGroupParameterAutoFix(PoseTuneDiagnostics.GroupGeneratedParameterConflict.Code, "この group の明示 parameter 名をクリア"),
            new AlignLoopSettingAutoFix(),
            new GenerateThumbnailAutoFix(),
            new ConvertLegacyTrackingPolicyAutoFix(),
            new DisableFbtGuardAutoFix(),
            new AllowFbtAutoFix(),
            new FillKawaiiSourceMotionAutoFix()
        };

        public IReadOnlyList<IPoseTuneAutoFix> Fixes => fixes;

        public IEnumerable<IPoseTuneAutoFix> FindFixes(ValidationIssue issue, PoseGraph graph)
        {
            return fixes.Where(fix => fix.Code == issue.Code && fix.CanFix(issue, graph));
        }
    }

    internal sealed class ConvertLegacyTrackingPolicyAutoFix : PoseTuneAutoFixBase
    {
        public ConvertLegacyTrackingPolicyAutoFix() : base(
            PoseTuneDiagnostics.LegacyInlineTrackingPolicy.Code,
            "旧 tracking 値を PoseTrackingPolicy へ変換",
            AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseClip pose &&
                   pose.GetComponent<PoseTrackingPolicy>() == null &&
                   TrackingPolicyUtility.WasCustomizedFromPoseDefault(pose.tracking);
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is not PoseClip pose || pose.GetComponent<PoseTrackingPolicy>() != null)
            {
                return;
            }

            var legacyTracking = TrackingPolicyUtility.Copy(pose.tracking);
            Undo.RecordObject(pose, Label);
            var policy = Undo.AddComponent<PoseTrackingPolicy>(pose.gameObject);
            Undo.RecordObject(policy, Label);
            policy.tracking = legacyTracking;
            policy.useFullBodyTrackingOverride = false;
            policy.generateResetOnExit = true;
            pose.tracking = TrackingPolicyData.DefaultForPose();
            EditorUtility.SetDirty(pose);
            EditorUtility.SetDirty(policy);
        }
    }

    internal abstract class PoseTuneAutoFixBase : IPoseTuneAutoFix
    {
        protected PoseTuneAutoFixBase(string code, string label, AutoFixSafety safety)
        {
            Code = code;
            Label = label;
            Safety = safety;
        }

        public string Code { get; }
        public string Label { get; }
        public AutoFixSafety Safety { get; }
        public virtual bool IncludeInBatch => true;
        public abstract bool CanFix(ValidationIssue issue, PoseGraph graph);
        public abstract void Apply(ValidationIssue issue, PoseGraph graph);
    }

    internal sealed class DisableGroupAutoFix : PoseTuneAutoFixBase
    {
        public DisableGroupAutoFix(string code, string label) : base(code, label, AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseGroup group && group.includeInBuild;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is not PoseGroup group)
            {
                return;
            }

            Undo.RecordObject(group, Label);
            group.includeInBuild = false;
            EditorUtility.SetDirty(group);
        }
    }

    internal sealed class ClearGroupParameterAutoFix : PoseTuneAutoFixBase
    {
        public ClearGroupParameterAutoFix(string code, string label) : base(code, label, AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseGroup group && !string.IsNullOrWhiteSpace(group.parameterName);
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is not PoseGroup group)
            {
                return;
            }

            Undo.RecordObject(group, Label);
            group.parameterName = "";
            EditorUtility.SetDirty(group);
        }
    }

    internal sealed class AlignLoopSettingAutoFix : PoseTuneAutoFixBase
    {
        public AlignLoopSettingAutoFix() : base(PoseTuneDiagnostics.ClipLoopMismatch.Code, "PoseClip.loop を AnimationClip の loopTime に合わせる", AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseClip pose && pose.clip != null;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is not PoseClip pose || pose.clip == null)
            {
                return;
            }

            Undo.RecordObject(pose, Label);
            pose.loop = AnimationUtility.GetAnimationClipSettings(pose.clip).loopTime;
            EditorUtility.SetDirty(pose);
        }
    }

    internal sealed class GenerateThumbnailAutoFix : PoseTuneAutoFixBase
    {
        public GenerateThumbnailAutoFix() : base(PoseTuneDiagnostics.MissingThumbnail.Code, "missing thumbnail を生成", AutoFixSafety.RequiresAssetWrite)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseClip && graph?.RootComponent != null;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is PoseClip pose && graph?.RootComponent != null)
            {
                new PoseTuneThumbnailGenerationService().Generate(pose, graph.RootComponent);
            }
        }
    }

    internal sealed class DisableDuplicateRootPoliciesAutoFix : PoseTuneAutoFixBase
    {
        public DisableDuplicateRootPoliciesAutoFix() : base(PoseTuneDiagnostics.DuplicateRootTrackingPolicies.Code, "2件目以降の root-level policy を無効化", AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return graph?.RootComponent != null && RootPolicies(graph.RootComponent).Count() > 1;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (graph?.RootComponent == null)
            {
                return;
            }

            foreach (var policy in RootPolicies(graph.RootComponent).Skip(1))
            {
                Undo.RecordObject(policy, Label);
                policy.enabled = false;
                EditorUtility.SetDirty(policy);
            }
        }

        private static IEnumerable<PoseTrackingPolicy> RootPolicies(PoseTuneRoot root)
        {
            return root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                .Where(policy => policy != null && IsRootLevelPolicy(root, policy))
                .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                .OrderBy(policy => policy.transform == root.transform ? 0 : 1)
                .ThenBy(policy => policy.transform.GetSiblingIndex());
        }

        private static bool IsRootLevelPolicy(PoseTuneRoot root, PoseTrackingPolicy policy)
        {
            if (policy.transform == root.transform)
            {
                return true;
            }

            return policy.transform.parent == root.transform &&
                   policy.GetComponent<PoseGroup>() == null &&
                   policy.GetComponent<PoseClip>() == null;
        }
    }

    internal sealed class DisableFbtGuardAutoFix : PoseTuneAutoFixBase
    {
        public DisableFbtGuardAutoFix() : base(PoseTuneDiagnostics.FullBodyTrackingDisabled.Code, "FBT 抑止 guard を生成しない", AutoFixSafety.Reversible)
        {
        }

        public override bool IncludeInBatch => false;

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return graph?.RootComponent != null && graph.RootComponent.disableWhenFullBodyTracking;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (graph?.RootComponent == null)
            {
                return;
            }

            Undo.RecordObject(graph.RootComponent, Label);
            graph.RootComponent.disableWhenFullBodyTracking = false;
            EditorUtility.SetDirty(graph.RootComponent);
        }
    }

    internal sealed class AllowFbtAutoFix : PoseTuneAutoFixBase
    {
        public AllowFbtAutoFix() : base(PoseTuneDiagnostics.FullBodyTrackingDisabled.Code, "FBT 互換モードを許可する", AutoFixSafety.Reversible)
        {
        }

        public override bool IncludeInBatch => false;

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return graph?.RootComponent != null && !graph.RootComponent.advancedSettings.allowFullBodyTracking;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (graph?.RootComponent == null)
            {
                return;
            }

            Undo.RecordObject(graph.RootComponent, Label);
            graph.RootComponent.advancedSettings.allowFullBodyTracking = true;
            EditorUtility.SetDirty(graph.RootComponent);
        }
    }

    internal sealed class FillKawaiiSourceMotionAutoFix : PoseTuneAutoFixBase
    {
        public FillKawaiiSourceMotionAutoFix() : base(PoseTuneDiagnostics.KawaiiSourceMotionMissing.Code, "clip を Kawaii sourceMotion に設定", AutoFixSafety.Reversible)
        {
        }

        public override bool CanFix(ValidationIssue issue, PoseGraph graph)
        {
            return issue.Context is PoseClip pose &&
                   pose.compatibilityProfile == PoseSourceCompatibilityProfile.KawaiiPosing &&
                   pose.sourceMotion == null &&
                   pose.clip != null;
        }

        public override void Apply(ValidationIssue issue, PoseGraph graph)
        {
            if (issue.Context is not PoseClip pose || pose.clip == null)
            {
                return;
            }

            Undo.RecordObject(pose, Label);
            pose.sourceMotion = pose.clip;
            EditorUtility.SetDirty(pose);
        }
    }
}
