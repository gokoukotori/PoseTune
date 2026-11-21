using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTunePoseOutputValidator
    {
        public static void Validate(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            if (pose.BlendMode == PoseClipBlendMode.Additive &&
                (pose.RootOffset != Vector3.zero || PoseTuneCompilerRules.AppliesHeightOffset(graph)))
            {
                report.Warning(PoseTuneDiagnostics.AdditivePoseOutputOffset.Code, "additive pose に root/height offset が含まれます。", pose.Source);
            }
        }
    }
}
