using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor.Compiler.Hashing
{
    internal static class PoseTuneGraphHasher
    {
        public static string Compute(PoseGraph graph)
        {
            var builder = new StringBuilder();
            AppendRoot(builder, graph);
            AppendOptions(builder, graph);
            AppendMenu(builder, graph?.Menu);
            AppendHeight(builder, graph);
            foreach (var group in graph != null
                         ? PoseGraphBuildFilter.BuildableGroups(graph)
                         : Enumerable.Empty<PoseGroupDefinition>())
            {
                AppendGroup(builder, group);
            }

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            var hash = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hash.Append(b.ToString("x2"));
            }

            return hash.ToString();
        }

        private static void AppendRoot(StringBuilder builder, PoseGraph graph)
        {
            var root = graph?.RootComponent;
            Append(builder, "rootGuid", root != null ? root.StableGuid : "");
            Append(builder, "displayName", root != null ? root.displayName : "");
            Append(builder, "parameterNamespace", root != null ? root.parameterNamespace : "");
            Append(builder, "buildMode", root != null ? root.buildMode.ToString() : "");
            Append(builder, "targetLayer", root != null ? root.targetLayer.ToString() : "");
            Append(builder, "enableAutoContextSwitch", root != null && root.enableAutoContextSwitch);
            Append(builder, "defaultMode", root != null ? root.defaultMode.ToString() : PoseTuneDefaultMode.Off.ToString());
            Append(builder, "poseSelectionSyncMode", root != null ? root.poseSelectionSyncMode.ToString() : PoseSelectionSyncMode.DirectGroupParameter.ToString());
            Append(builder, "poseWriteDefaultsMode", root != null ? root.poseWriteDefaultsMode.ToString() : PoseWriteDefaultsMode.PoseTuneDefault.ToString());
            Append(builder, "enableHeightAdjust", root != null && root.enableHeightAdjust);
            Append(builder, "questLowMemoryMode", root != null && root.questLowMemoryMode);
            Append(builder, "disableWhenFullBodyTracking", root != null && root.disableWhenFullBodyTracking);
            Append(builder, "advanced.allowFullBodyTracking", root != null && root.advancedSettings.allowFullBodyTracking);
            Append(builder, "advanced.lockDesktopLowerBodyTracking", root != null && root.advancedSettings.lockDesktopLowerBodyTracking);
            Append(builder, "advanced.actionWeightControlMode",
                root != null ? root.advancedSettings.actionWeightControlMode.ToString() : "");
            Append(builder, "trackingGuardProfile",
                root != null ? TrackingGuardCompiler.RootEntryProfile(root).ToString() : "");
            Append(builder, "root.hasCustomRootTrackingPolicy", graph != null && graph.HasCustomRootTrackingPolicy);
            Append(builder, "root.hasCustomRootGenerateResetOnExit", graph != null && graph.HasCustomRootGenerateResetOnExit);
            Append(builder, "root.generateResetOnExit", graph != null && graph.RootGenerateResetOnExit);
            AppendGoroneSystemExCompatibility(builder, graph);
            AppendTracking(builder, graph != null ? graph.RootTrackingPolicy : null);
        }

        private static void AppendGoroneSystemExCompatibility(StringBuilder builder, PoseGraph graph)
        {
            var compatibility = graph?.GoroneSystemExCompatibility;
            Append(builder, "gorone.active", graph != null && graph.HasGoroneSystemExGuard);
            Append(builder, "gorone.guardMode", compatibility != null ? compatibility.guardMode.ToString() : "");
            Append(builder, "gorone.requireGoroneSystemEx", compatibility != null && compatibility.requireGoroneSystemEx);
            Append(builder, "gorone.overridePoseTuneLayerPriority",
                compatibility != null && compatibility.overridePoseTuneLayerPriority);
            Append(builder, "gorone.poseTuneLayerPriority", compatibility != null ? compatibility.poseTuneLayerPriority : 0);
        }

        private static void AppendOptions(StringBuilder builder, PoseGraph graph)
        {
            Append(builder, "options.hasPoseOptions", graph != null && graph.HasPoseOptions);
            var options = graph?.Options ?? new PoseTuneOptions();
            Append(builder, "options.lockHead", options.lockHead);
            Append(builder, "options.lockHands", options.lockHands);
            Append(builder, "options.lockFeet", options.lockFeet);
            Append(builder, "options.locomotionLock", options.locomotionLock);
        }

        private static void AppendMenu(StringBuilder builder, PoseMenu menu)
        {
            Append(builder, "menu.rootMenuName", menu != null ? menu.rootMenuName : "");
            Append(builder, "menu.installMode", menu != null ? menu.installMode.ToString() : MenuInstallMode.AppendToRoot.ToString());
            Append(builder, "menu.autoSplitMenu", menu != null && menu.autoSplitMenu);
            Append(builder, "menu.generateIcons", menu != null && menu.generateIcons);
            Append(builder, "menu.useSubMenusPerGroup", menu != null && menu.useSubMenusPerGroup);
            Append(builder, "menu.lyingMenuLayout", menu != null ? menu.lyingMenuLayout.ToString() : LyingMenuLayout.CombinedLyingMenu.ToString());
        }

        private static void AppendHeight(StringBuilder builder, PoseGraph graph)
        {
            var height = graph?.HeightAdjust;
            Append(builder, "height.includeInBuild", PoseTuneAuthoringInclusion.Includes(height));
            Append(builder, "height.parameterName",
                graph != null ? PoseTuneNames.HeightParameter(graph.RootComponent, height) : "");
            Append(builder, "height.min", height != null ? height.min : 0f);
            Append(builder, "height.max", height != null ? height.max : 0f);
            Append(builder, "height.applyMode", height != null ? height.applyMode.ToString() : "");
            Append(builder, "height.blendProfile", height != null ? height.blendProfile.ToString() : "");
            Append(builder, "height.lowOffset", height != null ? height.lowOffset : 0f);
            Append(builder, "height.midOffset", height != null ? height.midOffset : 0f);
            Append(builder, "height.highOffset", height != null ? height.highOffset : 0f);
            Append(builder, "height.autoCorrectionMode", height != null ? height.autoCorrectionMode.ToString() : "");
            Append(builder, "height.referenceEyeHeightMeters", height != null ? height.referenceEyeHeightMeters : 0f);
            Append(builder, "height.maxAutoOffset", height != null ? height.maxAutoOffset : 0f);
            Append(builder, "height.generateRadialMenu", height != null && height.generateRadialMenu);
            Append(builder, "height.saved", height != null && height.saved);
            Append(builder, "height.synced", height != null && height.synced);
        }

        private static void AppendGroup(StringBuilder builder, PoseGroupDefinition group)
        {
            Append(builder, "group.id", group.Id);
            Append(builder, "group.displayName", group.DisplayName);
            Append(builder, "group.icon", AssetIdentity(group.Icon));
            Append(builder, "group.kind", group.Kind.ToString());
            Append(builder, "group.layerName", group.LayerName);
            Append(builder, "group.parameterName", group.ParameterName);
            Append(builder, "group.menuOrder", group.MenuOrder);
            Append(builder, "group.saved", group.Saved);
            Append(builder, "group.synced", group.Synced);
            Append(builder, "group.exclusive", group.Exclusive);
            Append(builder, "group.activationMode", group.ActivationMode.ToString());
            Append(builder, "group.autoPoseSelectionMode", group.AutoPoseSelectionMode.ToString());
            Append(builder, "group.autoContextProfile", group.AutoContextProfile.ToString());
            Append(builder, "group.emitTrackingControl", group.EmitTrackingControl);
            Append(builder, "group.suppressIconGeneration", group.SuppressIconGeneration);
            AppendConditions(builder, "group.condition", group.Conditions);
            foreach (var pose in group.Poses)
            {
                AppendPose(builder, pose);
            }
        }

        private static void AppendPose(StringBuilder builder, PoseDefinition pose)
        {
            Append(builder, "pose.id", pose.Id);
            Append(builder, "pose.displayName", pose.DisplayName);
            Append(builder, "pose.clip", AssetIdentity(pose.Clip));
            Append(builder, "pose.sourceMotion", AssetIdentity(pose.SourceMotion));
            Append(builder, "pose.compatibilityProfile", pose.CompatibilityProfile.ToString());
            Append(builder, "pose.adjustmentClip", AssetIdentity(pose.AdjustmentClip));
            Append(builder, "pose.adjustmentApplyMode", pose.AdjustmentApplyMode.ToString());
            Append(builder, "pose.rootYawOffsetDegrees", pose.RootYawOffsetDegrees);
            Append(builder, "pose.humanoidOrientationOffsetYDegrees", pose.HumanoidOrientationOffsetYDegrees);
            Append(builder, "pose.recenterRootXZToHead", pose.RecenterRootXZToHead);
            Append(builder, "pose.icon", AssetIdentity(pose.Icon));
            Append(builder, "pose.rootOffset", pose.RootOffset);
            Append(builder, "pose.cameraOffset", pose.CameraOffset);
            Append(builder, "pose.menuValue", pose.MenuValue);
            Append(builder, "pose.sourceSyncedParameterValue", pose.SourceSyncedParameterValue);
            Append(builder, "pose.initial", pose.Initial);
            Append(builder, "pose.loop", pose.Loop);
            Append(builder, "pose.menuOrder", pose.MenuOrder);
            Append(builder, "pose.priority", pose.Priority.ToString());
            Append(builder, "pose.blendMode", pose.BlendMode.ToString());
            Append(builder, "pose.generateResetOnExit", pose.GenerateResetOnExit);
            Append(builder, "pose.emitTrackingControl", pose.EmitTrackingControl);
            Append(builder, "pose.suppressIconGeneration", pose.SuppressIconGeneration);
            AppendTracking(builder, pose.TrackingPolicy);
            Append(builder, "pose.hasFullBodyTrackingOverride", pose.HasFullBodyTrackingOverride);
            if (pose.HasFullBodyTrackingOverride)
            {
                AppendTracking(builder, pose.FullBodyTrackingPolicy);
            }

            AppendPoseSpace(builder, pose.PoseSpace);
            AppendMotionTime(builder, pose.MotionTime);
            AppendConditions(builder, "pose.condition", pose.Conditions);
            foreach (var branch in pose.ConditionBranches)
            {
                AppendConditions(builder, "pose.conditionBranch", branch);
            }
        }

        private static void AppendTracking(StringBuilder builder, TrackingPolicyData tracking)
        {
            tracking ??= TrackingPolicyData.DefaultForPose();
            Append(builder, "tracking.head", tracking.head.ToString());
            Append(builder, "tracking.leftHand", tracking.leftHand.ToString());
            Append(builder, "tracking.rightHand", tracking.rightHand.ToString());
            Append(builder, "tracking.hip", tracking.hip.ToString());
            Append(builder, "tracking.leftFoot", tracking.leftFoot.ToString());
            Append(builder, "tracking.rightFoot", tracking.rightFoot.ToString());
            Append(builder, "tracking.leftFingers", tracking.leftFingers.ToString());
            Append(builder, "tracking.rightFingers", tracking.rightFingers.ToString());
            Append(builder, "tracking.eyes", tracking.eyes.ToString());
            Append(builder, "tracking.mouth", tracking.mouth.ToString());
        }

        private static void AppendPoseSpace(StringBuilder builder, PoseSpacePolicy poseSpace)
        {
            poseSpace ??= new PoseSpacePolicy();
            Append(builder, "poseSpace.enabled", poseSpace.enabled);
            Append(builder, "poseSpace.scope", poseSpace.scope.ToString());
            Append(builder, "poseSpace.enterPoseSpace", poseSpace.enterPoseSpace);
            Append(builder, "poseSpace.fixedDelay", poseSpace.fixedDelay);
            Append(builder, "poseSpace.delayTime", poseSpace.delayTime);
        }

        private static void AppendMotionTime(StringBuilder builder, MotionTimeSettings motionTime)
        {
            motionTime ??= new MotionTimeSettings();
            Append(builder, "motionTime.mode", motionTime.mode.ToString());
            Append(builder, "motionTime.parameterName", motionTime.parameterName);
            Append(builder, "motionTime.generateRadialMenu", motionTime.generateRadialMenu);
        }

        private static void AppendConditions(
            StringBuilder builder,
            string prefix,
            IEnumerable<ParameterConditionData> conditions)
        {
            foreach (var condition in conditions ?? Enumerable.Empty<ParameterConditionData>())
            {
                Append(builder, prefix + ".parameter", condition.parameter);
                Append(builder, prefix + ".valueType", condition.valueType.ToString());
                Append(builder, prefix + ".op", condition.op.ToString());
                Append(builder, prefix + ".floatValue", condition.floatValue);
                Append(builder, prefix + ".intValue", condition.intValue);
                Append(builder, prefix + ".boolValue", condition.boolValue);
            }
        }

        private static string AssetIdentity(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "";
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrWhiteSpace(path))
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                var identity = string.IsNullOrWhiteSpace(guid) ? path : guid;
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                return identity + ":" + dependencyHash;
            }

            try
            {
                return asset.GetType().FullName + ":" + asset.name + ":" + EditorJsonUtility.ToJson(asset);
            }
            catch (Exception)
            {
                return asset.GetType().FullName + ":" + asset.name;
            }
        }

        private static void Append(StringBuilder builder, string key, object value)
        {
            builder.Append(key).Append('=').Append(value ?? "").Append('\n');
        }
    }
}
