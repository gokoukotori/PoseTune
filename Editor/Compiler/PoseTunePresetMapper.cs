using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTunePresetMapper
    {
        public static PoseGroupPresetData CaptureGroup(PoseTuneRoot root, PoseGroup group)
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
                trackingPolicy = CaptureTrackingPolicy(group.GetComponents<PoseTrackingPolicy>().FirstOrDefault()),
                suppressIconGeneration = group.suppressIconGeneration,
                groupConditions = group.groupConditions.Select(PoseTuneConditionUtility.Copy).ToList(),
                poseSpace = CopyPoseSpace(group.poseSpace),
                poses = PoseGroupOwnership.OwnedClips(group)
                    .Where(pose => pose.GetComponentInParent<PoseTuneRoot>(true) == root)
                    .Where(PoseTuneAuthoringInclusion.Includes)
                    .OrderBy(pose => pose.menuOrder)
                    .ThenBy(pose => pose.displayName)
                    .Select(CapturePose)
                    .ToList()
            };
        }

        public static void ApplyGroupData(PoseTuneRoot root, PoseGroup group, PoseGroupPresetData data)
        {
            var requestedStableGuid = NormalizeGuid(data.groupStableGuid);
            if (!string.IsNullOrEmpty(requestedStableGuid) &&
                !StableGuidExistsOnOtherGroup(root, group, requestedStableGuid))
            {
                group.SetStableGuid(requestedStableGuid);
            }
            else if (string.IsNullOrEmpty(requestedStableGuid) &&
                     string.IsNullOrEmpty(ReadStableGuid(group)))
            {
                group.RegenerateStableGuid();
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
            var requestedStableGuid = NormalizeGuid(data.poseStableGuid);
            if (!string.IsNullOrEmpty(requestedStableGuid) &&
                !StableGuidExistsOnOtherPose(root, pose, requestedStableGuid))
            {
                pose.SetStableGuid(requestedStableGuid);
            }
            else if (string.IsNullOrEmpty(requestedStableGuid) &&
                     string.IsNullOrEmpty(ReadStableGuid(pose)))
            {
                pose.RegenerateStableGuid();
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
            var policy = pose.GetComponents<PoseTrackingPolicy>().FirstOrDefault();
            var policyData = policy != null
                ? CaptureTrackingPolicy(policy)
                : TrackingPolicyUtility.WasCustomizedFromPoseDefault(pose.tracking)
                    ? new PoseTrackingPolicyPresetData
                    {
                        present = true,
                        tracking = TrackingPolicyUtility.Copy(pose.tracking),
                        useFullBodyTrackingOverride = false,
                        fullBodyTracking = TrackingPolicyData.DefaultForPose(),
                        generateResetOnExit = true
                    }
                    : new PoseTrackingPolicyPresetData { present = false };
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
                tracking = TrackingPolicyData.DefaultForPose(),
                emitTrackingControl = pose.emitTrackingControl,
                trackingPolicy = policyData,
                suppressIconGeneration = pose.suppressIconGeneration,
                motionTime = CopyMotionTime(pose.motionTime),
                poseSpace = CopyPoseSpace(pose.poseSpace),
                clipConditions = pose.clipConditions.Select(PoseTuneConditionUtility.Copy).ToList()
            };
        }

        public static PoseTrackingPolicyPresetData CaptureTrackingPolicy(PoseTrackingPolicy policy)
        {
            if (policy == null)
            {
                return new PoseTrackingPolicyPresetData { present = false };
            }

            return new PoseTrackingPolicyPresetData
            {
                present = true,
                tracking = TrackingPolicyUtility.Copy(policy.tracking),
                useFullBodyTrackingOverride = policy.useFullBodyTrackingOverride,
                fullBodyTracking = TrackingPolicyUtility.Copy(policy.fullBodyTracking),
                generateResetOnExit = policy.generateResetOnExit
            };
        }

        public static bool TrackingPolicyMatches(
            PoseTrackingPolicy policy,
            PoseTrackingPolicyPresetData data)
        {
            if (policy == null || data == null || !data.present || !policy.enabled)
            {
                return false;
            }

            return TrackingPolicyUtility.AreEqual(policy.tracking, data.tracking) &&
                   policy.useFullBodyTrackingOverride == data.useFullBodyTrackingOverride &&
                   TrackingPolicyUtility.AreEqual(policy.fullBodyTracking, data.fullBodyTracking) &&
                   policy.generateResetOnExit == data.generateResetOnExit;
        }

        public static void ApplyTrackingPolicyData(
            PoseTrackingPolicy policy,
            PoseTrackingPolicyPresetData data)
        {
            if (policy == null || data == null || !data.present)
            {
                return;
            }

            policy.tracking = TrackingPolicyUtility.Copy(data.tracking);
            policy.useFullBodyTrackingOverride = data.useFullBodyTrackingOverride;
            policy.fullBodyTracking = TrackingPolicyUtility.Copy(data.fullBodyTracking);
            policy.generateResetOnExit = data.generateResetOnExit;
            policy.enabled = true;
        }

        private static bool StableGuidExistsOnOtherGroup(PoseTuneRoot root, PoseGroup current, string stableGuid)
        {
            var normalized = NormalizeGuid(stableGuid);
            return !string.IsNullOrEmpty(normalized) &&
                   root.GetComponentsInChildren<PoseGroup>(true)
                       .Where(group => group.GetComponentInParent<PoseTuneRoot>(true) == root)
                       .Any(group => group != current && ReadStableGuid(group) == normalized);
        }

        private static bool StableGuidExistsOnOtherPose(PoseTuneRoot root, PoseClip current, string stableGuid)
        {
            var normalized = NormalizeGuid(stableGuid);
            return !string.IsNullOrEmpty(normalized) &&
                   root.GetComponentsInChildren<PoseClip>(true)
                       .Where(pose => pose.GetComponentInParent<PoseTuneRoot>(true) == root)
                       .Any(pose => pose != current && ReadStableGuid(pose) == normalized);
        }

        private static string ReadStableGuid(UnityEngine.Object component)
        {
            if (component == null)
            {
                return "";
            }

            using var serialized = new SerializedObject(component);
            return NormalizeGuid(serialized.FindProperty("stableGuid.value")?.stringValue);
        }

        private static string NormalizeGuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return Guid.TryParse(value.Trim(), out var guid)
                ? guid.ToString("N")
                : value.Trim();
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
