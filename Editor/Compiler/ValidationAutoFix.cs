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
        Destructive
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
            new DisableFbtGuardAutoFix(),
            new AllowFbtAutoFix(),
            new FillKawaiiSourceMotionAutoFix()
        };

        public IEnumerable<IPoseTuneAutoFix> FindFixes(ValidationIssue issue, PoseGraph graph)
        {
            return fixes.Where(fix => fix.Code == issue.Code && fix.CanFix(issue, graph));
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

        protected void Modify(Object target, System.Action change)
        {
            Undo.RecordObject(target, Label);
            change();
            EditorUtility.SetDirty(target);
        }
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

            Modify(group, () => group.includeInBuild = false);
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

            Modify(group, () => group.parameterName = "");
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

            Modify(pose, () => pose.loop = AnimationUtility.GetAnimationClipSettings(pose.clip).loopTime);
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

            Modify(graph.RootComponent, () => graph.RootComponent.disableWhenFullBodyTracking = false);
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

            Modify(graph.RootComponent, () => graph.RootComponent.advancedSettings.allowFullBodyTracking = true);
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

            Modify(pose, () => pose.sourceMotion = pose.clip);
        }
    }
}
