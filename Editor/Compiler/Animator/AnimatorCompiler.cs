using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private const float CriticalStateHoldSeconds = 0.02f;

        public AnimatorBuildResult Compile(PoseGraph graph, ParameterPlan parameters)
        {
            graph.TrackingVotes = new TrackingVoteRegistry();
            var result = AnimatorControllerFactory.CreateBuildResult(graph, parameters);
            CreateActionPoseLayers(result, graph);
            FxAssistCompiler.Compile(result, graph);

            return result;
        }

        private static void CreateActionPoseLayers(AnimatorBuildResult result, PoseGraph graph)
        {
            var controlsActionPlayable = PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent);
            const bool tracksGroupActivity = true;
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    CreateActionPoseLayer(result, graph, group, bucket.Poses, bucket.LayerName, bucket.BlendMode,
                        tracksGroupActivity, PoseTuneNames.GroupActiveParameter(group, bucket.BlendMode));
                }

                AddHigherPriorityAutoPreemptionTransitions(result.TargetController, graph, group);
            }

            if (controlsActionPlayable)
            {
                ActionWeightLayerCompiler.Compile(result, graph);
            }

            if (ParameterAllocator.NeedsTrackingArbiter(graph))
            {
                TrackingArbiterCompiler.Compile(result, graph);
            }

            PoseOptionsLayerCompiler.Compile(result, graph);
        }
    }
}

