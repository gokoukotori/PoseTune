using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using nadena.dev.modular_avatar.core;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneGoroneSystemExValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            var compatibility = graph.GoroneSystemExCompatibility;
            if (!graph.HasGoroneSystemExGuard || compatibility == null)
            {
                return;
            }

            if (graph.GoroneSystemExCompatibilityCount > 1)
            {
                report.Warning(
                    PoseTuneDiagnostics.MultipleGoroneSystemExCompatibility.Code,
                    "Gorone System EX Compatibility が複数あります。最初の component だけを使用します。",
                    compatibility);
            }

            var detection = GoroneSystemExDetector.Detect(graph.AvatarRoot);
            if (!detection.HasGoroneSystemEx)
            {
                if (detection.HasSupineSystem)
                {
                    report.Warning(
                        PoseTuneDiagnostics.ClassicSupineDetected.Code,
                        "通常版 Gorone/Supine らしき構成は見つかりましたが、Gorone System EX の marker が見つかりません。EX 互換ガードの対象外として扱います。",
                        compatibility);
                }

                if (compatibility.requireGoroneSystemEx)
                {
                    report.Error(
                        PoseTuneDiagnostics.GoroneSystemExMissing.Code,
                        "Gorone System EX Compatibility が有効ですが、Gorone System EX が検出できません。",
                        compatibility);
                }
                else
                {
                    report.Warning(
                        PoseTuneDiagnostics.GoroneSystemExMissing.Code,
                        "Gorone System EX Compatibility が有効ですが、Gorone System EX が検出できません。",
                        compatibility);
                }
            }

            if (detection.HasGoroneSystemEx && detection.VrcSupineParameters.Count == 0)
            {
                report.Error(
                    PoseTuneDiagnostics.VrcSupineParameterMissing.Code,
                    "`VRCSupine` の Modular Avatar parameter が見つかりません。Gorone System EX の状態を PoseTune が参照できません。",
                    compatibility);
            }

            foreach (var parameter in detection.VrcSupineParameters)
            {
                if (parameter.Config.syncType != ParameterSyncType.Int)
                {
                    report.Error(
                        PoseTuneDiagnostics.VrcSupineParameterTypeInvalid.Code,
                        "`VRCSupine` の Modular Avatar parameter は Int である必要があります。",
                        parameter.Component != null ? parameter.Component : compatibility);
                }
            }

            if (graph.RootComponent.targetLayer == PoseTuneTargetLayer.Base)
            {
                report.Warning(
                    PoseTuneDiagnostics.GoroneSystemExBaseLayerConflictRisk.Code,
                    "PoseTune の対象レイヤーが Base です。Gorone System EX も Base を置換するため、Action layer 出力を推奨します。",
                    graph.RootComponent);
            }
        }
    }
}
