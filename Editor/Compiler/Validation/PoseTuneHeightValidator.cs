using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneHeightValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            if (graph.HeightAdjustCount > 1)
            {
                report.Error(PoseTuneDiagnostics.MultipleHeightAdjust.Code,
                    "ビルド対象の PoseHeightAdjust が複数あります。1 つだけ有効にしてください。",
                    graph.RootComponent);
            }

            var height = graph.HeightAdjust;
            if (height == null || height.autoCorrectionMode == HeightAutoCorrectionMode.Disabled)
            {
                return;
            }

            report.Warning(PoseTuneDiagnostics.HeightRuntimeAutoCorrectionRequiresVerification.Code, "runtime height auto correction は VRChat runtime での確認が必要です。", height);
            if (height.maxAutoOffset > 1f)
            {
                report.Warning(PoseTuneDiagnostics.HeightMaxAutoOffsetLarge.Code, "最大自動オフセットが大きすぎるため、床沈みや浮きの可能性があります。", height);
            }
        }
    }
}
