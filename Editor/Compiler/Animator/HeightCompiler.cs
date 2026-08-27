using System.Collections.Generic;
using System;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class HeightCompiler
    {
        public HeightBuildResult BuildMotionWithAssets(PoseDefinition pose, bool enabled, string parameterName)
        {
            return BuildMotionWithAssets(pose, enabled, parameterName, -1f, 1f);
        }

        public HeightBuildResult BuildMotionWithAssets(
            PoseDefinition pose,
            bool enabled,
            string parameterName,
            float minOffset,
            float maxOffset)
        {
            return BuildMotionWithAssets(
                pose,
                enabled,
                parameterName,
                minOffset,
                maxOffset,
                PoseMotionPreparationContext.Empty());
        }

        public HeightBuildResult BuildMotionWithAssets(
            PoseDefinition pose,
            bool enabled,
            string parameterName,
            float minOffset,
            float maxOffset,
            PoseMotionPreparationContext context)
        {
            return BuildMotionWithAssets(
                pose,
                enabled,
                parameterName,
                minOffset,
                0f,
                maxOffset,
                HeightApplyMode.RootOrHipsYOffset,
                null,
                context ?? PoseMotionPreparationContext.Empty());
        }

        public HeightBuildResult BuildMotionWithAssets(
            PoseDefinition pose,
            PoseHeightAdjust height,
            GameObject avatarRoot,
            PoseMotionPreparationContext context)
        {
            var enabled = height != null && height.applyMode != HeightApplyMode.Disabled;
            var rootComponent = context != null && context.RootComponent != null
                ? context.RootComponent
                : avatarRoot != null
                    ? avatarRoot.GetComponentInChildren<PoseTuneRoot>(true)
                    : null;
            var parameterName = PoseTuneNames.HeightParameter(rootComponent, height);
            var low = height != null ? height.lowOffset : -1f;
            var mid = height != null ? height.midOffset : 0f;
            var high = height != null ? height.highOffset : 1f;
            if (height == null || height.blendProfile == HeightBlendProfile.Standard)
            {
                low = height != null ? height.min : -1f;
                mid = 0f;
                high = height != null ? height.max : 1f;
            }

            return BuildMotionWithAssets(
                pose,
                enabled,
                parameterName,
                low,
                mid,
                high,
                height != null ? height.applyMode : HeightApplyMode.RootOrHipsYOffset,
                height,
                context ?? PoseMotionPreparationContext.Empty());
        }

        private HeightBuildResult BuildMotionWithAssets(
            PoseDefinition pose,
            bool enabled,
            string parameterName,
            float lowOffset,
            float midOffset,
            float highOffset,
            HeightApplyMode applyMode,
            PoseHeightAdjust height,
            PoseMotionPreparationContext context)
        {
            var source = pose?.SourceMotion != null ? pose.SourceMotion : pose?.Clip;
            if (source == null)
            {
                return new HeightBuildResult { Motion = null };
            }

            var prepared = PoseMotionPreparationService.PrepareMotion(
                pose,
                pose.DisplayName + "_Generated",
                context);
            if (!enabled)
            {
                return new HeightBuildResult
                {
                    Motion = prepared.Motion,
                    GeneratedAssets = prepared.GeneratedAssets
                };
            }

            var assets = new List<Object>(prepared.GeneratedAssets);
            var tree = BuildHeightMotion(
                prepared.Motion,
                pose.DisplayName,
                parameterName,
                lowOffset,
                midOffset,
                highOffset,
                applyMode,
                height,
                assets);
            return new HeightBuildResult
            {
                Motion = tree,
                GeneratedAssets = assets
            };
        }

        private static Motion BuildHeightMotion(
            Motion preparedMotion,
            string poseName,
            string parameterName,
            float lowOffset,
            float midOffset,
            float highOffset,
            HeightApplyMode applyMode,
            PoseHeightAdjust height,
            List<Object> assets)
        {
            if (!TryAutoCorrection(height, out var autoParameter, out var lowThreshold, out var midThreshold, out var highThreshold, out var maxOffset))
            {
                return BuildManualHeightBlend(
                    preparedMotion,
                    poseName + "_Height",
                    parameterName,
                    lowOffset,
                    midOffset,
                    highOffset,
                    applyMode,
                    0f,
                    assets);
            }

            var low = BuildManualHeightBlend(
                preparedMotion,
                poseName + "_HeightAutoLow",
                parameterName,
                lowOffset,
                midOffset,
                highOffset,
                applyMode,
                -maxOffset,
                assets);
            var mid = BuildManualHeightBlend(
                preparedMotion,
                poseName + "_HeightAutoMid",
                parameterName,
                lowOffset,
                midOffset,
                highOffset,
                applyMode,
                0f,
                assets);
            var high = BuildManualHeightBlend(
                preparedMotion,
                poseName + "_HeightAutoHigh",
                parameterName,
                lowOffset,
                midOffset,
                highOffset,
                applyMode,
                maxOffset,
                assets);
            var tree = new BlendTree
            {
                name = poseName + "_HeightAutoBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = autoParameter,
                useAutomaticThresholds = false
            };
            tree.AddChild(low, lowThreshold);
            tree.AddChild(mid, midThreshold);
            tree.AddChild(high, highThreshold);
            assets.Add(tree);
            return tree;
        }

        private static BlendTree BuildManualHeightBlend(
            Motion source,
            string name,
            string parameterName,
            float lowOffset,
            float midOffset,
            float highOffset,
            HeightApplyMode applyMode,
            float autoOffset,
            List<Object> assets)
        {
            var low = CloneHeightMotion(source, name + "Low", lowOffset + autoOffset, applyMode, assets);
            var mid = CloneHeightMotion(source, name + "Mid", midOffset + autoOffset, applyMode, assets);
            var high = CloneHeightMotion(source, name + "High", highOffset + autoOffset, applyMode, assets);
            var tree = new BlendTree
            {
                name = name + "Blend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = parameterName,
                useAutomaticThresholds = false
            };
            tree.AddChild(low, 0f);
            tree.AddChild(mid, 0.5f);
            tree.AddChild(high, 1f);
            assets.Add(tree);
            return tree;
        }

        private static bool TryAutoCorrection(
            PoseHeightAdjust height,
            out string parameterName,
            out float lowThreshold,
            out float midThreshold,
            out float highThreshold,
            out float maxOffset)
        {
            parameterName = "";
            lowThreshold = 0f;
            midThreshold = 0f;
            highThreshold = 0f;
            maxOffset = height != null ? Mathf.Max(0f, height.maxAutoOffset) : 0f;
            if (height == null || maxOffset <= 0f)
            {
                return false;
            }

            var referenceEyeHeight = Mathf.Max(0.0001f, height.referenceEyeHeightMeters);
            switch (height.autoCorrectionMode)
            {
                case HeightAutoCorrectionMode.RuntimeScaleFactor:
                    parameterName = "ScaleFactor";
                    var scaleDelta = maxOffset / referenceEyeHeight;
                    lowThreshold = Mathf.Max(0f, 1f - scaleDelta);
                    midThreshold = 1f;
                    highThreshold = 1f + scaleDelta;
                    return true;
                case HeightAutoCorrectionMode.RuntimeEyeHeightMeters:
                    parameterName = "EyeHeightAsMeters";
                    lowThreshold = Mathf.Max(0f, referenceEyeHeight - maxOffset);
                    midThreshold = referenceEyeHeight;
                    highThreshold = referenceEyeHeight + maxOffset;
                    return true;
                default:
                    return false;
            }
        }

        private static Motion CloneHeightMotion(
            Motion source,
            string name,
            float offset,
            HeightApplyMode applyMode,
            List<Object> assets)
        {
            Func<AnimationClip, string, AnimationClip> clipTransformer =
                applyMode == HeightApplyMode.HumanoidLevelOffset
                    ? (clip, cloneName) => CloneAnimationClipWithHumanoidLevelOffset(clip, cloneName, offset)
                    : (clip, cloneName) => CloneAnimationClipWithRootYOffset(clip, cloneName, offset);
            return MotionTreeCloneUtility.Clone(source, name, assets, clipTransformer);
        }

        private static AnimationClip CloneAnimationClipWithHumanoidLevelOffset(
            AnimationClip clip,
            string name,
            float offset)
        {
            var cloned = Object.Instantiate(clip);
            cloned.name = name;
            var settings = AnimationUtility.GetAnimationClipSettings(cloned);
            settings.level += offset;
            AnimationUtility.SetAnimationClipSettings(cloned, settings);
            return cloned;
        }

        private static AnimationClip CloneAnimationClipWithRootYOffset(
            AnimationClip clip,
            string name,
            float offset)
        {
            var cloned = Object.Instantiate(clip);
            cloned.name = name;
            ApplyRootYOffset(cloned, offset);
            return cloned;
        }

        private static void ApplyRootYOffset(AnimationClip clip, float offset)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
            var existing = AnimationUtility.GetEditorCurve(clip, binding);
            var curve = existing != null
                ? PoseRootMotionCurveUtility.OffsetCurve(existing, offset)
                : AnimationCurve.Constant(0f, Mathf.Max(0f, clip.length), offset);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
