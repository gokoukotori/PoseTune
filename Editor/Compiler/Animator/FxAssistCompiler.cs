namespace Gokoukotori.PoseTune.Editor
{
    internal static class FxAssistCompiler
    {
        public static bool HasContent(PoseGraph graph)
        {
            return false;
        }

        public static void Compile(AnimatorBuildResult result, PoseGraph graph)
        {
            // Extension point for future non-transform FX helpers. Intentionally no-op for 21章対応.
        }
    }
}
