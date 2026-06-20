using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Validation;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseValidator
    {
        public ValidationReport Validate(PoseGraph graph)
        {
            var report = new ValidationReport();
            if (!PoseTuneRootValidator.ValidatePrerequisites(graph, report))
            {
                return report;
            }

            PoseTuneStableGuidValidator.Validate(graph, report);
            var context = PoseTuneValidationContext.Create(graph);

            PoseTuneGroupValidator.ValidateGroups(graph, report);

            PoseTuneTrackingValidator.ValidateRootPolicies(graph, report);
            PoseTuneGroupValidator.ValidateParameterConflicts(graph, report);
            PoseTuneMenuValidator.Validate(context, report);
            PoseTuneGroupValidator.ValidateSyncedGroupIntCount(graph, report);

            foreach (var pose in graph.Poses)
            {
                PoseTuneKawaiiCompatibilityValidator.ValidatePose(graph, pose, report);
                if (pose.Clip == null && pose.SourceMotion == null)
                {
                    report.Error(PoseTuneDiagnostics.ClipMotionMissing.Code, "PoseClip に AnimationClip がありません。", pose.Source);
                    continue;
                }

                PoseTuneClipValidator.ValidateMotion(pose, report);

                PoseTuneMotionTimeValidator.Validate(graph, pose, report);
                PoseTuneTrackingValidator.ValidatePose(graph, pose, report);
                PoseTunePoseOutputValidator.Validate(graph, pose, report);
                PoseTuneIconValidator.Validate(graph, pose, report);
            }

            PoseTuneParameterValidator.Validate(context, report);
            PoseTuneHeightValidator.Validate(graph, report);
            PoseTuneKawaiiCompatibilityValidator.ValidateGraph(graph, report);
            PoseTuneGoroneSystemExValidator.Validate(graph, report);
            PoseTuneLayerCompatibilityValidator.Validate(graph, report);
            PoseTuneTrackingValidator.ValidateFbtCompatibility(graph, report);

            graph.Validation = report;
            return report;
        }

    }
}
