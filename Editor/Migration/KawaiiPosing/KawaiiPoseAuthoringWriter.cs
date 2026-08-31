using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPoseAuthoringWriter
    {
        public static PoseClip CreatePose(
            PoseGroup group,
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            bool iconsDisabled,
            string undoName,
            IKawaiiMigrationAssetStore assetStore = null)
        {
            var poseObject = new GameObject(SafeName(KawaiiPosingMapper.DisplayName(source), "Pose " + source.Index));
            Undo.RegisterCreatedObjectUndo(poseObject, undoName);
            Undo.SetTransformParent(poseObject.transform, group.transform, undoName);
            var pose = Undo.AddComponent<PoseClip>(poseObject);
            ConfigurePose(pose, group, source, options, report, iconsDisabled, assetStore);
            report.Created(poseObject, "Pose");
            return pose;
        }

        internal static void ConfigurePose(
            PoseClip pose,
            PoseGroup group,
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            bool iconsDisabled,
            IKawaiiMigrationAssetStore assetStore = null)
        {
            var sourceMotion = ResolveSourceMotion(source.Motion, source.Clip, options, report);
            var skippedBlendTree = IsSkippedBlendTreeWithoutFallback(source, options, sourceMotion);
            pose.displayName = KawaiiPosingMapper.DisplayName(source);
            pose.includeInBuild = source.Enabled && !skippedBlendTree;
            pose.clip = source.Clip;
            pose.sourceMotion = sourceMotion;
            pose.adjustmentClip = source.AdjustmentClip;
            pose.loop = ResolveSourceLoop(sourceMotion, source.Clip);
            pose.customIcon = ResolvePoseIcon(source, options, iconsDisabled);
            pose.isInitial = options.preserveInitialPose && source.Initial;
            pose.explicitMenuValue = options.preserveExplicitMenuValues ? source.TypeParameterValue : 0;
            pose.sourceSyncedParameterValue = source.SyncedParameterValue;
            pose.menuOrder = source.Index;
            pose.compatibilityProfile = PoseSourceCompatibilityProfile.KawaiiPosing;
            pose.adjustmentApplyMode = options.adjustmentMode == KawaiiAdjustmentMode.AdditiveCompatible
                ? PoseAdjustmentApplyMode.AdditiveKawaiiCompatible
                : PoseAdjustmentApplyMode.ReplaceCurves;
            pose.rootYawOffsetDegrees = 0f;
            pose.humanoidOrientationOffsetYDegrees =
                options.rotationMode == KawaiiRotationMode.HumanoidOrientationOffsetY && source.IsRotate
                    ? source.Rotate
                    : 0f;
            pose.recenterRootXZToHead = options.rootRecenterMode == KawaiiRootRecenterMode.FirstRootKeyApproximation;
            ApplyMotionTime(pose, source, options, report);
            ApplyPoseSpace(pose, options);
            BakeCompatibilityAtMigration(pose, source, options, report, assetStore);

            if (pose.sourceMotion == null)
            {
                if (pose.includeInBuild)
                {
                    report.Error(PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code, "build 対象 pose の animation clip が見つかりません: " + pose.displayName, pose);
                }
                else
                {
                    report.Warning(PoseTuneDiagnostics.KawaiiPoseSourceMotionMissing.Code, "animation clip が見つかりません: " + pose.displayName, pose);
                }
            }
        }

        private static Texture2D ResolvePoseIcon(
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            bool iconsDisabled)
        {
            if (iconsDisabled || !options.preserveCustomIcons)
            {
                return null;
            }

            if (source.IsCustomIcon && source.Icon != null)
            {
                return source.Icon;
            }

            return source.PreviewImage;
        }

        internal static Motion ResolveSourceMotion(
            Motion motion,
            AnimationClip clip,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            if (motion is BlendTree)
            {
                switch (options.blendTreeMode)
                {
                    case KawaiiBlendTreeMode.Skip:
                        report.Warning(PoseTuneDiagnostics.KawaiiBlendTreePoseSkipped.Code, "BlendTree pose は Skip により build 対象外にしました。", motion);
                        return null;
                    case KawaiiBlendTreeMode.FlattenLeaves:
                        report.Warning(PoseTuneDiagnostics.KawaiiBlendTreeFlattenFallback.Code, "BlendTree flatten 対象に leaf clip がないため fallback motion を保持します。", motion);
                        return clip != null ? clip : motion;
                    default:
                        return motion;
                }
            }

            return motion != null ? motion : clip;
        }

        internal static bool IsSkippedBlendTreeWithoutFallback(
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            Motion resolvedMotion)
        {
            return options.blendTreeMode == KawaiiBlendTreeMode.Skip &&
                   source != null &&
                   source.BlendTree != null &&
                   resolvedMotion == null;
        }

        private static void ApplyMotionTime(
            PoseClip pose,
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            if (!source.IsMotionTime)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(source.MotionTimeParameterName))
            {
                report.Warning(PoseTuneDiagnostics.KawaiiMotionTimeParameterMissing.Code, "MotionTime parameter が空です: " + pose.displayName, pose);
                return;
            }

            pose.motionTime.parameterName = source.MotionTimeParameterName.Trim();
            switch (options.motionTimeMode)
            {
                case KawaiiMotionTimeMode.CustomFloatParameterWithRadialMenu:
                    pose.motionTime.mode = MotionTimeMode.UseCustomFloatParameter;
                    pose.motionTime.generateRadialMenu = true;
                    break;
                case KawaiiMotionTimeMode.AnimatorOnlyParameter:
                    pose.motionTime.mode = MotionTimeMode.UseAnimatorStateTimeParameter;
                    pose.motionTime.generateRadialMenu = false;
                    break;
            }
        }

        private static void ApplyPoseSpace(PoseClip pose, KawaiiMigrationOptions options)
        {
            switch (options.poseSpaceMode)
            {
                case KawaiiPoseSpaceMode.Off:
                    pose.poseSpace.enabled = false;
                    break;
                case KawaiiPoseSpaceMode.DesktopOnlyCompatible:
                    pose.poseSpace.enabled = true;
                    pose.poseSpace.scope = PoseSpaceScope.DesktopOnly;
                    pose.poseSpace.enterPoseSpace = true;
                    break;
                case KawaiiPoseSpaceMode.PoseTuneDefault:
                    pose.poseSpace.enabled = true;
                    pose.poseSpace.scope = PoseSpaceScope.All;
                    pose.poseSpace.enterPoseSpace = true;
                    break;
            }
        }

        private static void BakeCompatibilityAtMigration(
            PoseClip pose,
            KawaiiAnimationDto source,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            IKawaiiMigrationAssetStore assetStore)
        {
            var bakeRootRecenter = options.rootRecenterMode == KawaiiRootRecenterMode.BakeAtMigration;
            var bakeRotation = options.rotationMode == KawaiiRotationMode.BakeAtMigration && source.IsRotate;
            if (!bakeRootRecenter && !bakeRotation)
            {
                return;
            }

            var definition = new PoseDefinition
            {
                Id = PoseTuneObjectIdentity.BuildKey(pose, pose.transform.root),
                DisplayName = pose.displayName,
                Clip = pose.clip,
                SourceMotion = pose.sourceMotion,
                AdjustmentClip = pose.adjustmentClip,
                CompatibilityProfile = pose.compatibilityProfile,
                AdjustmentApplyMode = pose.adjustmentApplyMode,
                RootYawOffsetDegrees = pose.rootYawOffsetDegrees,
                HumanoidOrientationOffsetYDegrees = bakeRotation ? source.Rotate : 0f,
                RecenterRootXZToHead = bakeRootRecenter,
                Loop = pose.loop,
                Source = pose
            };

            var prepared = KawaiiRootMotionCompatibilityBaker.Prepare(
                definition,
                pose.displayName + "_KawaiiBaked",
                PoseMotionPreparationContext.Empty());
            if (prepared.Motion == null)
            {
                throw new System.InvalidOperationException("BakeAtMigration did not produce a Motion: " + pose.displayName);
            }

            if (assetStore == null)
            {
                throw new System.InvalidOperationException("BakeAtMigration requires a persistent migration asset store.");
            }

            var persistedMotion = assetStore.PersistPreparedMotion(pose, prepared);
            if (persistedMotion == null || !EditorUtility.IsPersistent(persistedMotion))
            {
                throw new System.InvalidOperationException("BakeAtMigration Motion was not persisted: " + pose.displayName);
            }

            pose.sourceMotion = persistedMotion;
            pose.clip = persistedMotion as AnimationClip ?? pose.clip;
            pose.adjustmentClip = null;
            if (bakeRootRecenter)
            {
                pose.rootOffset = Vector3.zero;
                pose.rootYawOffsetDegrees = 0f;
                pose.recenterRootXZToHead = false;
            }

            if (bakeRotation)
            {
                pose.humanoidOrientationOffsetYDegrees = 0f;
            }
        }

        public static bool ResolveSourceLoop(Motion sourceMotion, AnimationClip fallbackClip)
        {
            var leafClips = LeafClips(sourceMotion != null ? sourceMotion : fallbackClip).ToList();
            return leafClips.Count > 0 &&
                   leafClips.All(clip => AnimationUtility.GetAnimationClipSettings(clip).loopTime);
        }

        private static IEnumerable<AnimationClip> LeafClips(Motion motion)
        {
            if (motion is AnimationClip clip)
            {
                yield return clip;
                yield break;
            }

            if (motion is not BlendTree tree)
            {
                yield break;
            }

            foreach (var child in tree.children)
            {
                foreach (var leaf in LeafClips(child.motion))
                {
                    yield return leaf;
                }
            }
        }

        private static string SafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
