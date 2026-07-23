using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateFactory
    {
        public static PoseStateVariants CreateVariants(
            AnimatorBuildResult result,
            AnimatorControllerLayer layer,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            HashSet<string> duplicateStateBaseNames,
            Vector3 position,
            bool controlsActionPlayable,
            string activeParameter,
            string poseActiveParameter)
        {
            var needsPoseSpaceVrVariant = PoseStateVariantRules.NeedsPoseSpaceVrVariant(pose);
            var needsDesktopLowerBodyLockVariant =
                PoseStateVariantRules.NeedsDesktopLowerBodyLockVariant(graph.RootComponent, group, pose);
            var baseTrackingPolicy = PoseStateVariantRules.LocksExistingDesktopPoseState(
                graph.RootComponent,
                group,
                pose)
                ? PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(pose.TrackingPolicy)
                : pose.TrackingPolicy;
            var desktopLowerBodyTrackingPolicy =
                PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(pose.TrackingPolicy);
            var motionResult = BuildMotion(result, graph, pose);
            result.GeneratedAssets.AddRange(motionResult.GeneratedAssets);
            var hasFullBodyTrackingVariant = pose.HasFullBodyTrackingOverride &&
                                             graph.RootComponent.advancedSettings.allowFullBodyTracking;
            baseTrackingPolicy = EffectiveTrackingPolicy(pose, baseTrackingPolicy);
            desktopLowerBodyTrackingPolicy = EffectiveTrackingPolicy(pose, desktopLowerBodyTrackingPolicy);
            var vrTrackingPolicy = EffectiveTrackingPolicy(pose, pose.TrackingPolicy);
            var fullBodyTrackingPolicy = EffectiveTrackingPolicy(pose, pose.FullBodyTrackingPolicy);

            var variants = new PoseStateVariants
            {
                NeedsPoseSpaceVrVariant = needsPoseSpaceVrVariant,
                NeedsDesktopLowerBodyLockVariant = needsDesktopLowerBodyLockVariant,
                BaseTrackingPolicy = baseTrackingPolicy,
                DesktopLowerBodyTrackingPolicy = desktopLowerBodyTrackingPolicy,
                VrTrackingPolicy = vrTrackingPolicy,
                FullBodyTrackingPolicy = fullBodyTrackingPolicy,
                BaseTrackingVoteId = TrackingVoteId(graph, group, pose, "Base", baseTrackingPolicy),
                DesktopLowerBodyTrackingVoteId = needsDesktopLowerBodyLockVariant
                    ? TrackingVoteId(graph, group, pose, "Desktop", desktopLowerBodyTrackingPolicy)
                    : 0,
                VrTrackingVoteId = needsPoseSpaceVrVariant
                    ? TrackingVoteId(graph, group, pose, "VR", vrTrackingPolicy)
                    : 0,
                FullBodyTrackingVoteId = hasFullBodyTrackingVariant
                    ? TrackingVoteId(graph, group, pose, "FBT", fullBodyTrackingPolicy)
                    : 0
            };

            variants.BaseState = CreateState(
                layer,
                graph,
                pose,
                PoseStateNaming.Name(pose, duplicateStateBaseNames),
                position,
                motionResult,
                true,
                controlsActionPlayable,
                activeParameter,
                poseActiveParameter,
                group,
                variants.BaseTrackingVoteId);

            if (needsDesktopLowerBodyLockVariant)
            {
                variants.DesktopLowerBodyState = CreateState(
                    layer,
                    graph,
                    pose,
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_Desktop"),
                    new Vector3(position.x + 280, position.y),
                    motionResult,
                    true,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    group,
                    variants.DesktopLowerBodyTrackingVoteId);
            }

            if (needsPoseSpaceVrVariant)
            {
                variants.VrState = CreateState(
                    layer,
                    graph,
                    pose,
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_VR"),
                    new Vector3(position.x + (needsDesktopLowerBodyLockVariant ? 560 : 280), position.y),
                    motionResult,
                    false,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    group,
                    variants.VrTrackingVoteId);
            }

            if (hasFullBodyTrackingVariant)
            {
                variants.FullBodyState = CreateState(
                    layer,
                    graph,
                    pose,
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_FBT"),
                    new Vector3(position.x + (needsDesktopLowerBodyLockVariant ? 840 : 560), position.y),
                    motionResult,
                    !needsPoseSpaceVrVariant,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    group,
                    variants.FullBodyTrackingVoteId);
            }

            return variants;
        }

        private static AnimatorState CreateState(
            AnimatorControllerLayer layer,
            PoseGraph graph,
            PoseDefinition pose,
            string stateName,
            Vector3 position,
            HeightBuildResult motionResult,
            bool enterPoseSpace,
            bool controlsActionPlayable,
            string activeParameter,
            string poseActiveParameter,
            PoseGroupDefinition group,
            int trackingVoteId)
        {
            var state = layer.stateMachine.AddState(stateName, position);
            state.motion = motionResult.Motion;
            ApplyMotionTime(state, graph, pose);
            state.writeDefaultValues = WriteDefaultsForPose(graph.RootComponent);
            if (enterPoseSpace)
            {
                PoseSpaceCompiler.AddEnterPoseSpaceBehavior(state, pose.PoseSpace);
            }

            if (controlsActionPlayable)
            {
                ParameterDriverCompiler.SetGroupActive(state, activeParameter, 1f);
            }

            if (trackingVoteId > 0)
            {
                ParameterDriverCompiler.SetTrackingVote(state, group, trackingVoteId);
            }

            ParameterDriverCompiler.SetPoseActive(state, poseActiveParameter, 1f);
            return state;
        }

        private static int TrackingVoteId(
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            string variant,
            TrackingPolicyData policy)
        {
            if (graph == null || !ParameterAllocator.NeedsTrackingArbiter(graph) || pose == null)
            {
                return 0;
            }

            if (!pose.EmitTrackingControl)
            {
                return 0;
            }

            return graph.TrackingVotes.GetOrAdd(group, pose, variant, policy);
        }

        private static TrackingPolicyData EffectiveTrackingPolicy(
            PoseDefinition pose,
            TrackingPolicyData policy)
        {
            return pose != null && pose.EmitTrackingControl
                ? TrackingPolicyUtility.Copy(policy)
                : TrackingPolicyUtility.NoChange();
        }

        private static HeightBuildResult BuildMotion(
            AnimatorBuildResult result,
            PoseGraph graph,
            PoseDefinition pose)
        {
            var heightEnabled = graph.RootComponent.enableHeightAdjust &&
                                PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                                graph.HeightAdjust.applyMode != HeightApplyMode.Disabled;
            if (heightEnabled)
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
                graph.HeightAdjust != null ? graph.HeightAdjust.max : 1f);
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
