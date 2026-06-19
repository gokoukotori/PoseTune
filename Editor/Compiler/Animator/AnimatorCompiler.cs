using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private const float CriticalStateHoldSeconds = 0.02f;

        public AnimatorBuildResult Compile(PoseGraph graph, ParameterPlan parameters)
        {
            var result = AnimatorControllerFactory.CreateBuildResult(graph, parameters);
            CreateActionPoseLayers(result, graph);
            FxAssistCompiler.Compile(result, graph);

            return result;
        }

        private static void CreateActionPoseLayers(AnimatorBuildResult result, PoseGraph graph)
        {
            var controlsActionPlayable = PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent);
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    CreateActionPoseLayer(result, graph, group, bucket.Poses, bucket.LayerName, bucket.BlendMode,
                        controlsActionPlayable, PoseTuneNames.GroupActiveParameter(group, bucket.BlendMode));
                }
            }

            if (controlsActionPlayable)
            {
                ActionWeightLayerCompiler.Compile(result, graph);
            }

            CompileTrackingResetLayer(result);
            if (graph.HasPoseOptions)
            {
                PoseOptionsLayerCompiler.Compile(result, graph);
            }
        }
    }
}

