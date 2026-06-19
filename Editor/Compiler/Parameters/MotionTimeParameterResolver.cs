using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum MotionTimeParameterUsage
    {
        AnimatorState,
        RadialMenu,
        ParameterValidation
    }

    internal readonly struct MotionTimeParameterResolution
    {
        public MotionTimeParameterResolution(
            string parameterName,
            Object context,
            bool usesGeneratedHeight)
        {
            ParameterName = parameterName ?? "";
            Context = context;
            UsesGeneratedHeight = usesGeneratedHeight;
        }

        public bool HasParameter => !string.IsNullOrWhiteSpace(ParameterName);
        public string ParameterName { get; }
        public Object Context { get; }
        public bool UsesGeneratedHeight { get; }

        public static MotionTimeParameterResolution None =>
            new("", null, false);
    }

    internal static class MotionTimeParameterResolver
    {
        public static MotionTimeParameterResolution Resolve(
            PoseGraph graph,
            PoseDefinition pose,
            MotionTimeParameterUsage usage)
        {
            if (pose?.MotionTime == null)
            {
                return MotionTimeParameterResolution.None;
            }

            switch (pose.MotionTime.mode)
            {
                case MotionTimeMode.UseAnimatorStateTimeParameter:
                    return usage == MotionTimeParameterUsage.RadialMenu
                        ? MotionTimeParameterResolution.None
                        : Explicit(pose);
                case MotionTimeMode.UseCustomFloatParameter:
                    return Explicit(pose);
                case MotionTimeMode.UseGeneratedHeightParameter:
                    return usage == MotionTimeParameterUsage.ParameterValidation
                        ? MotionTimeParameterResolution.None
                        : GeneratedHeight(graph);
                default:
                    return MotionTimeParameterResolution.None;
            }
        }

        private static MotionTimeParameterResolution Explicit(PoseDefinition pose)
        {
            return new MotionTimeParameterResolution(
                pose.MotionTime.parameterName,
                pose.Source,
                false);
        }

        private static MotionTimeParameterResolution GeneratedHeight(PoseGraph graph)
        {
            if (graph == null ||
                graph.HeightAdjust == null ||
                !ParameterAllocator.NeedsGeneratedHeightParameter(graph))
            {
                return MotionTimeParameterResolution.None;
            }

            return new MotionTimeParameterResolution(
                PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust),
                graph.HeightAdjust,
                true);
        }
    }
}
