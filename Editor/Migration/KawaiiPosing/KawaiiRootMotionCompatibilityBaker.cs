namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiRootMotionCompatibilityBaker
    {
        public static PoseMotionPreparationResult Prepare(
            PoseDefinition pose,
            string name,
            PoseMotionPreparationContext context)
        {
            return PoseMotionPreparationService.PrepareMotion(pose, name, context);
        }
    }
}
