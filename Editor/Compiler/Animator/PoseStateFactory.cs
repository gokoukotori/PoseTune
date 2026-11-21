using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateFactory
    {
        public static PoseStateVariants CreateVariants(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            string poseActiveParameter,
            int x,
            int y)
        {
            var group = context.Group;
            var variants = PoseStateVariantRules.Plan(context.Root, group, pose);
            var motionResult = BuildMotion(context.Result, context.Graph, pose);
            context.Result.GeneratedAssets.AddRange(motionResult.GeneratedAssets);
            // Tracking votes are allocated in variant order, before any state is created.
            foreach (var variant in variants.All)
            {
                variant.Policy = EffectiveTrackingPolicy(
                    group,
                    VariantTrackingPolicy(context.Root, group, pose, variant.Kind));
                variant.VoteId = TrackingVoteId(context.Graph, group, variant.Policy);
            }

            foreach (var variant in variants.All)
            {
                variant.State = CreateState(
                    context,
                    pose,
                    variant,
                    new Vector3(x + variant.StateX, y),
                    motionResult,
                    poseActiveParameter);
            }

            return variants;
        }

        private static AnimatorState CreateState(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariant variant,
            Vector3 position,
            HeightBuildResult motionResult,
            string poseActiveParameter)
        {
            var state = AnimatorLayerFactory.AddState(
                context.Layer.stateMachine,
                PoseStateNaming.Name(pose, context.DuplicateStateBaseNames, variant.Suffix),
                position,
                motionResult.Motion);
            ApplyMotionTime(state, context.Graph, pose);
            state.writeDefaultValues = WriteDefaultsForPose(context.Root);
            if (variant.AppliesPoseSpace)
            {
                PoseSpaceCompiler.AddEnterPoseSpaceBehavior(state, pose.PoseSpace);
            }

            ParameterDriverCompiler.SetGroupActive(state, context.ActiveParameter, 1f);

            if (variant.VoteId > 0)
            {
                ParameterDriverCompiler.SetTrackingVote(state, context.Group, variant.VoteId);
            }

            ParameterDriverCompiler.SetPoseActive(state, poseActiveParameter, 1f);
            return state;
        }

        private static TrackingPolicyData VariantTrackingPolicy(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseStateVariantKind kind)
        {
            switch (kind)
            {
                case PoseStateVariantKind.Base:
                    return PoseStateVariantRules.LocksExistingDesktopPoseState(root, group, pose)
                        ? PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(group.TrackingPolicy)
                        : group.TrackingPolicy;
                case PoseStateVariantKind.Desktop:
                    return PoseStateVariantRules.DesktopVariantTrackingPolicy(root, group, pose);
                case PoseStateVariantKind.Vr:
                    return group.TrackingPolicy;
                default:
                    return group.FullBodyTrackingPolicy;
            }
        }

        private static int TrackingVoteId(
            PoseGraph graph,
            PoseGroupDefinition group,
            TrackingPolicyData policy)
        {
            if (graph == null || !ParameterAllocator.NeedsTrackingArbiter(graph) || group == null)
            {
                return 0;
            }

            if (!group.EmitTrackingControl)
            {
                return 0;
            }

            return graph.TrackingVotes.GetOrAdd(group, policy);
        }

        private static TrackingPolicyData EffectiveTrackingPolicy(
            PoseGroupDefinition group,
            TrackingPolicyData policy)
        {
            return group != null && group.EmitTrackingControl
                ? TrackingPolicyUtility.Copy(policy)
                : TrackingPolicyUtility.NoChange();
        }

        private static HeightBuildResult BuildMotion(
            AnimatorBuildResult result,
            PoseGraph graph,
            PoseDefinition pose)
        {
            if (PoseTuneCompilerRules.AppliesHeightOffset(graph))
            {
                return new HeightCompiler().BuildMotionWithAssets(
                    pose,
                    graph.HeightAdjust,
                    graph.AvatarRoot,
                    PoseMotionPreparationContext.FromGraph(graph, result));
            }

            return new HeightCompiler().BuildMotionWithAssets(
                pose,
                false,
                PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust),
                graph.HeightAdjust != null ? graph.HeightAdjust.min : -1f,
                graph.HeightAdjust != null ? graph.HeightAdjust.max : 1f,
                PoseMotionPreparationContext.FromGraph(graph, result));
        }

        private static void ApplyMotionTime(AnimatorState state, PoseGraph graph, PoseDefinition pose)
        {
            var resolution = MotionTimeParameterResolver.Resolve(
                graph,
                pose,
                MotionTimeParameterUsage.AnimatorState);
            if (!resolution.HasParameter)
            {
                return;
            }

            state.timeParameterActive = true;
            state.timeParameter = resolution.ParameterName;
        }

        private static bool WriteDefaultsForPose(PoseTuneRoot root)
        {
            switch (root != null ? root.poseWriteDefaultsMode : PoseWriteDefaultsMode.PoseTuneDefault)
            {
                case PoseWriteDefaultsMode.ForceOff:
                    return false;
                case PoseWriteDefaultsMode.ForceOn:
                    return true;
                default:
                    return true;
            }
        }

    }
}
