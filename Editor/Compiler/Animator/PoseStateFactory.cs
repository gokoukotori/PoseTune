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

            var variants = new PoseStateVariants
            {
                NeedsPoseSpaceVrVariant = needsPoseSpaceVrVariant,
                NeedsDesktopLowerBodyLockVariant = needsDesktopLowerBodyLockVariant,
                BaseTrackingContextId = TrackingContextId(graph, pose, baseTrackingPolicy),
                DesktopLowerBodyTrackingContextId = needsDesktopLowerBodyLockVariant
                    ? TrackingContextId(graph, pose, desktopLowerBodyTrackingPolicy)
                    : 0,
                VrTrackingContextId = needsPoseSpaceVrVariant
                    ? TrackingContextId(graph, pose, pose.TrackingPolicy)
                    : 0,
                FullBodyTrackingContextId = hasFullBodyTrackingVariant
                    ? TrackingContextId(graph, pose, pose.FullBodyTrackingPolicy)
                    : 0
            };

            variants.BaseState = CreateState(
                layer,
                graph,
                pose,
                PoseStateNaming.Name(pose, duplicateStateBaseNames),
                position,
                motionResult,
                baseTrackingPolicy,
                true,
                controlsActionPlayable,
                activeParameter,
                poseActiveParameter,
                variants.BaseTrackingContextId);

            if (needsDesktopLowerBodyLockVariant)
            {
                variants.DesktopLowerBodyState = CreateState(
                    layer,
                    graph,
                    pose,
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_Desktop"),
                    new Vector3(position.x + 280, position.y),
                    motionResult,
                    desktopLowerBodyTrackingPolicy,
                    true,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    variants.DesktopLowerBodyTrackingContextId);
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
                    pose.TrackingPolicy,
                    false,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    variants.VrTrackingContextId);
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
                    pose.FullBodyTrackingPolicy,
                    !needsPoseSpaceVrVariant,
                    controlsActionPlayable,
                    activeParameter,
                    poseActiveParameter,
                    variants.FullBodyTrackingContextId);
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
            TrackingPolicyData trackingPolicy,
            bool enterPoseSpace,
            bool controlsActionPlayable,
            string activeParameter,
            string poseActiveParameter,
            int trackingContextId)
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

            if (trackingContextId > 0)
            {
                ParameterDriverCompiler.SetTrackingContext(state, trackingContextId);
            }

            ParameterDriverCompiler.SetPoseActive(state, poseActiveParameter, 1f);
            return state;
        }

        private static int TrackingContextId(PoseGraph graph, PoseDefinition pose, TrackingPolicyData policy)
        {
            if (graph == null || !ParameterAllocator.NeedsTrackingContext(graph) || pose == null)
            {
                return 0;
            }

            if (!pose.EmitTrackingControl && !graph.HasPoseOptions)
            {
                return 0;
            }

            var effectivePolicy = pose.EmitTrackingControl
                ? policy
                : TrackingPolicyUtility.NoChange();
            return graph.TrackingContexts.GetOrAdd(effectivePolicy);
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
