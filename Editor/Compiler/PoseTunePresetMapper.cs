using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTunePresetMapper
    {
        public static PoseGroupPresetData CaptureGroup(PoseGroup group)
        {
            return new PoseGroupPresetData
            {
                groupStableGuid = group.StableGuid,
                kind = group.kind,
                displayName = group.displayName,
                parameterName = group.parameterName,
                menuOrder = group.menuOrder,
                icon = group.icon,
                exclusive = group.exclusive,
                saved = group.saved,
                synced = group.synced,
                activationMode = group.activationMode,
                autoPoseSelectionMode = group.autoPoseSelectionMode,
                autoContextProfile = group.autoContextProfile,
                emitTrackingControl = group.emitTrackingControl,
                suppressIconGeneration = group.suppressIconGeneration,
                groupConditions = group.groupConditions.Select(PoseTuneConditionUtility.Copy).ToList(),
                poseSpace = CopyPoseSpace(group.poseSpace),
                poses = group.GetComponentsInChildren<PoseClip>(true)
                    .Where(PoseTuneAuthoringInclusion.Includes)
                    .OrderBy(pose => pose.menuOrder)
                    .ThenBy(pose => pose.displayName)
                    .Select(CapturePose)
                    .ToList()
            };
        }

        public static void ApplyGroupData(PoseTuneRoot root, PoseGroup group, PoseGroupPresetData data)
        {
            if (!StableGuidExistsOnOtherGroup(root, group, data.groupStableGuid))
            {
                group.SetStableGuid(data.groupStableGuid);
            }

            group.includeInBuild = true;
            ((Behaviour)group).enabled = true;
            group.kind = data.kind;
            group.displayName = string.IsNullOrWhiteSpace(data.displayName)
                ? PoseTuneTemplateFactory.DefaultDisplayName(data.kind)
                : data.displayName;
            group.parameterName = data.parameterName;
            group.menuOrder = data.menuOrder;
            group.icon = data.icon;
            group.exclusive = data.exclusive;
            group.saved = data.saved;
            group.synced = data.synced;
            group.activationMode = data.activationMode;
            group.autoPoseSelectionMode = data.autoPoseSelectionMode;
            group.autoContextProfile = data.autoContextProfile;
            group.emitTrackingControl = data.emitTrackingControl;
            group.suppressIconGeneration = data.suppressIconGeneration;
            group.groupConditions = (data.groupConditions ?? new List<ParameterConditionData>())
                .Select(PoseTuneConditionUtility.Copy)
                .ToList();
            group.poseSpace = CopyPoseSpace(data.poseSpace);
        }

        public static void ApplyPoseData(PoseTuneRoot root, PoseClip pose, PoseClipPresetData data)
        {
            if (!StableGuidExistsOnOtherPose(root, pose, data.poseStableGuid))
            {
                pose.SetStableGuid(data.poseStableGuid);
            }

            pose.includeInBuild = true;
            ((Behaviour)pose).enabled = true;
            pose.displayName = data.displayName;
            pose.clip = data.clip;
            pose.sourceMotion = data.sourceMotion;
            pose.compatibilityProfile = data.compatibilityProfile;
            pose.adjustmentClip = data.adjustmentClip;
            pose.adjustmentApplyMode = data.adjustmentApplyMode;
            pose.customIcon = data.icon;
            pose.rootYawOffsetDegrees = data.rootYawOffsetDegrees;
            pose.humanoidOrientationOffsetYDegrees = data.humanoidOrientationOffsetYDegrees;
            pose.recenterRootXZToHead = data.recenterRootXZToHead;
            pose.menuOrder = data.menuOrder;
            pose.isInitial = data.isInitial;
            pose.loop = data.loop;
            pose.explicitMenuValue = data.explicitMenuValue;
            pose.sourceSyncedParameterValue = data.sourceSyncedParameterValue;
            pose.rootOffset = data.rootOffset;
            pose.cameraOffset = data.cameraOffset;
            pose.priority = data.priority;
            pose.blendMode = data.blendMode;
            pose.tracking = TrackingPolicyUtility.Copy(data.tracking);
            pose.emitTrackingControl = data.emitTrackingControl;
            pose.suppressIconGeneration = data.suppressIconGeneration;
            pose.motionTime = CopyMotionTime(data.motionTime);
            pose.poseSpace = CopyPoseSpace(data.poseSpace);
            pose.clipConditions = (data.clipConditions ?? new List<ParameterConditionData>())
                .Select(PoseTuneConditionUtility.Copy)
                .ToList();
        }

        private static PoseClipPresetData CapturePose(PoseClip pose)
        {
            return new PoseClipPresetData
            {
                poseStableGuid = pose.StableGuid,
                displayName = pose.displayName,
                clip = pose.clip,
                sourceMotion = pose.sourceMotion,
                compatibilityProfile = pose.compatibilityProfile,
                adjustmentClip = pose.adjustmentClip,
                adjustmentApplyMode = pose.adjustmentApplyMode,
                icon = pose.customIcon,
                rootYawOffsetDegrees = pose.rootYawOffsetDegrees,
                humanoidOrientationOffsetYDegrees = pose.humanoidOrientationOffsetYDegrees,
                recenterRootXZToHead = pose.recenterRootXZToHead,
                menuOrder = pose.menuOrder,
                isInitial = pose.isInitial,
                loop = pose.loop,
                explicitMenuValue = pose.explicitMenuValue,
                sourceSyncedParameterValue = pose.sourceSyncedParameterValue,
                rootOffset = pose.rootOffset,
                cameraOffset = pose.cameraOffset,
                priority = pose.priority,
                blendMode = pose.blendMode,
                tracking = TrackingPolicyUtility.Copy(pose.tracking),
                emitTrackingControl = pose.emitTrackingControl,
                suppressIconGeneration = pose.suppressIconGeneration,
                motionTime = CopyMotionTime(pose.motionTime),
                poseSpace = CopyPoseSpace(pose.poseSpace),
                clipConditions = pose.clipConditions.Select(PoseTuneConditionUtility.Copy).ToList()
            };
        }

        private static bool StableGuidExistsOnOtherGroup(PoseTuneRoot root, PoseGroup current, string stableGuid)
        {
            return !string.IsNullOrWhiteSpace(stableGuid) &&
                   root.GetComponentsInChildren<PoseGroup>(true)
                       .Any(group => group != current && group.StableGuid == stableGuid);
        }

        private static bool StableGuidExistsOnOtherPose(PoseTuneRoot root, PoseClip current, string stableGuid)
        {
            return !string.IsNullOrWhiteSpace(stableGuid) &&
                   root.GetComponentsInChildren<PoseClip>(true)
                       .Any(pose => pose != current && pose.StableGuid == stableGuid);
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
    }
}
