using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseGroupCollector
    {
        public static PoseGroupDefinition Collect(
            PoseTuneRoot root,
            PoseGraph graph,
            PoseGroup group,
            HashSet<string> usedParameters,
            HashSet<string> usedLayerNames)
        {
            var tracking = PoseTuneTrackingPolicyResolver.ResolveGroupPolicy(root, group);
            var definition = new PoseGroupDefinition
            {
                Id = PoseTuneObjectIdentity.BuildKey(
                    group,
                    graph.AvatarRoot != null ? graph.AvatarRoot.transform : root.transform.root),
                DisplayName = string.IsNullOrWhiteSpace(group.displayName) ? group.name : group.displayName,
                Kind = group.kind,
                MenuOrder = group.menuOrder,
                Saved = group.saved,
                Synced = group.synced,
                Exclusive = group.exclusive,
                ActivationMode = group.activationMode,
                AutoPoseSelectionMode = group.autoPoseSelectionMode,
                AutoContextProfile = group.autoContextProfile,
                EmitTrackingControl = group.emitTrackingControl,
                GenerateResetOnExit = tracking.GenerateResetOnExit,
                TrackingPolicy = tracking.Policy,
                HasFullBodyTrackingOverride = tracking.HasFullBodyTrackingOverride,
                FullBodyTrackingPolicy = tracking.FullBodyTrackingPolicy,
                Icon = group.icon,
                Source = group,
                Conditions = new List<ParameterConditionData>(group.groupConditions)
            };

            var explicitValues = new HashSet<int>();
            var nextValue = 1;
            foreach (var clip in PoseGroupOwnership.OwnedClips(group)
                         .Where(PoseTuneAuthoringInclusion.Includes)
                         .OrderBy(p => p.menuOrder)
                         .ThenBy(p => p.displayName)
                         .ThenBy(p => p.name))
            {
                var effectiveMotion = clip.sourceMotion != null ? clip.sourceMotion : clip.clip;
                var effectiveClip = effectiveMotion as AnimationClip ?? clip.clip;
                var value = clip.explicitMenuValue > 0 ? clip.explicitMenuValue : NextValue(explicitValues, ref nextValue);
                explicitValues.Add(value);
                var conditionBranches = PoseTuneConditionBranchCollector.Collect(graph, group, clip);
                var pose = new PoseDefinition
                {
                    Id = PoseTuneObjectIdentity.BuildKey(
                        clip,
                        graph.AvatarRoot != null ? graph.AvatarRoot.transform : root.transform.root),
                    DisplayName = string.IsNullOrWhiteSpace(clip.displayName)
                        ? (effectiveMotion != null ? effectiveMotion.name : clip.name)
                        : clip.displayName,
                    Clip = effectiveClip,
                    SourceMotion = effectiveMotion,
                    AdjustmentClip = clip.adjustmentClip,
                    CompatibilityProfile = clip.compatibilityProfile,
                    AdjustmentApplyMode = clip.adjustmentApplyMode,
                    RootYawOffsetDegrees = clip.rootYawOffsetDegrees,
                    HumanoidOrientationOffsetYDegrees = clip.humanoidOrientationOffsetYDegrees,
                    RecenterRootXZToHead = clip.recenterRootXZToHead,
                    RootOffset = clip.rootOffset,
                    Icon = clip.customIcon,
                    MenuValue = value,
                    SourceSyncedParameterValue = clip.sourceSyncedParameterValue,
                    Initial = clip.isInitial,
                    Loop = clip.loop,
                    MenuOrder = clip.menuOrder,
                    Group = definition,
                    Priority = clip.priority,
                    BlendMode = clip.blendMode,
                    PoseSpace = ResolvePoseSpace(group, clip),
                    MotionTime = CopyMotionTime(clip.motionTime),
                    Conditions = conditionBranches.SelectMany(branch => branch).ToList(),
                    ConditionBranches = conditionBranches,
                    Source = clip
                };
                definition.Poses.Add(pose);
            }

            PoseNameAllocator.AssignNames(root, group, definition, usedParameters, usedLayerNames);
            return definition;
        }

        private static int NextValue(HashSet<int> used, ref int next)
        {
            while (used.Contains(next))
            {
                next++;
            }

            return next++;
        }

        private static PoseSpacePolicy ResolvePoseSpace(PoseGroup group, PoseClip clip)
        {
            if (clip.poseSpace != null && clip.poseSpace.enabled)
            {
                return CopyPoseSpace(clip.poseSpace);
            }

            return CopyPoseSpace(group.poseSpace);
        }

        private static PoseSpacePolicy CopyPoseSpace(PoseSpacePolicy source)
        {
            source ??= new PoseSpacePolicy();
            return new PoseSpacePolicy
            {
                enabled = source.enabled,
                scope = source.scope,
                enterPoseSpace = source.enterPoseSpace,
                fixedDelay = source.fixedDelay,
                delayTime = source.delayTime
            };
        }

        private static MotionTimeSettings CopyMotionTime(MotionTimeSettings source)
        {
            source ??= new MotionTimeSettings();
            return new MotionTimeSettings
            {
                mode = source.mode,
                parameterName = source.parameterName,
                generateRadialMenu = source.generateRadialMenu
            };
        }
    }
}
