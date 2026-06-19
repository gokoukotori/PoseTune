using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneIconValidator
    {
        public static void Validate(PoseGraph graph, PoseDefinition pose, ValidationReport report)
        {
            if (graph.Menu != null && !graph.Menu.generateIcons)
            {
                return;
            }

            if (!graph.RootComponent.enableIconGeneration ||
                graph.RootComponent.questLowMemoryMode ||
                pose.Source == null ||
                pose.SuppressIconGeneration ||
                pose.Source.customIcon != null)
            {
                return;
            }

            if (new PoseTuneIconResolver().ResolvePoseIcon(graph, pose) == null)
            {
                report.Warning(PoseTuneDiagnostics.MissingThumbnail.Code, "アイコン生成が有効ですが cache 済み thumbnail がないため、build では null icon を使用します。", pose.Source);
            }
        }
    }
}
