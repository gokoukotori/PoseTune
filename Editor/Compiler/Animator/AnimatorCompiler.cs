using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class AnimatorCompiler
    {
        internal const float CriticalStateHoldSeconds = 0.02f;

        public AnimatorBuildResult Compile(PoseGraph graph, ParameterPlan parameters)
        {
            graph.TrackingVotes = new TrackingVoteRegistry();
            var result = AnimatorControllerFactory.CreateBuildResult(graph, parameters);
            CreateActionPoseLayers(result, graph);
            return result;
        }

        private static void CreateActionPoseLayers(AnimatorBuildResult result, PoseGraph graph)
        {
            var controlsActionPlayable = PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent);
            UnityEngine.AnimationClip handoffHold = null;
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    if (handoffHold == null)
                    {
                        handoffHold = AnimatorLayerFactory.ResetHoldClip(
                            "PT_HandoffHold",
                            CriticalStateHoldSeconds);
                        result.GeneratedAssets.Add(handoffHold);
                    }

                    PoseLayerCompiler.CreateActionPoseLayer(result, graph, group, bucket.Poses, bucket.LayerName, bucket.BlendMode,
                        PoseTuneNames.GroupActiveParameter(group, bucket.BlendMode), handoffHold);
                }

                AutoPreemptionCompiler.AddHigherPriorityAutoPreemptionTransitions(result.TargetController, graph, group);
            }

            UnityEngine.AnimationClip refreshLoop = null;
            if (PoseRefreshLayerCompiler.IsNeeded(graph))
            {
                refreshLoop = PoseRefreshLayerCompiler.CreateRefreshLoopClip("PT_PoseRefresh_Loop");
                result.GeneratedAssets.Add(refreshLoop);
            }

            if (controlsActionPlayable)
            {
                ActionWeightLayerCompiler.Compile(result, graph, refreshLoop);
            }

            if (ParameterAllocator.NeedsTrackingArbiter(graph))
            {
                TrackingArbiterCompiler.Compile(result, graph, refreshLoop);
            }

            if (refreshLoop != null)
            {
                PoseRefreshLayerCompiler.Compile(result);
            }

            PoseOptionsLayerCompiler.Compile(result, graph);
        }
    }
}

