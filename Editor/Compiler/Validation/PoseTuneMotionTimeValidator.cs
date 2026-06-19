using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneMotionTimeValidator
    {
        public static void Validate(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            if (pose.MotionTime == null || pose.MotionTime.mode == MotionTimeMode.None)
            {
                return;
            }

            if (pose.MotionTime.mode == MotionTimeMode.UseGeneratedHeightParameter)
            {
                if (!graph.RootComponent.enableHeightAdjust || !PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) ||
                    string.IsNullOrWhiteSpace(PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust)))
                {
                    report.Error(PoseTuneDiagnostics.MotionTimeInvalid.Code, "MotionTime には有効な高さパラメータが必要です。", pose.Source);
                }

                if (graph.RootComponent.enableHeightAdjust &&
                    PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                    graph.HeightAdjust.applyMode != HeightApplyMode.Disabled)
                {
                    report.Error(PoseTuneDiagnostics.MotionTimeGeneratedHeightConflict.Code,
                        "MotionTime の UseGeneratedHeightParameter では、高さパラメータがポーズの高さ調整にも使われないように Height Apply Mode を Disabled にしてください。",
                        pose.Source);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(pose.MotionTime.parameterName))
            {
                report.Error(PoseTuneDiagnostics.MotionTimeInvalid.Code, "MotionTime パラメータが未設定です。", pose.Source);
            }

            if (pose.MotionTime.mode == MotionTimeMode.UseAnimatorStateTimeParameter &&
                pose.MotionTime.generateRadialMenu)
            {
                report.Warning(PoseTuneDiagnostics.MotionTimeAnimatorStateMenuUnavailable.Code, "AnimatorStateTime の MotionTime パラメータは menu から操作できません。", pose.Source);
            }
        }
    }
}
