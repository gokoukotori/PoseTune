using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPosingMapper
    {
        public static PoseGroupKind MapGroupKind(KawaiiLayerDto layer)
        {
            var key = ((layer.StateMachineName ?? "") + " " + (layer.MenuName ?? "")).ToLowerInvariant();
            if (key.Contains("sitstand") || key.Contains("standing") || key.Contains("立ち"))
            {
                return PoseGroupKind.Standing;
            }

            if (key.Contains("sitshallow") || key.Contains("chair") || key.Contains("中腰"))
            {
                return PoseGroupKind.Chair;
            }

            if (key.Contains("sitdeep") || key.Contains("floor") || key.Contains("座り"))
            {
                return PoseGroupKind.Floor;
            }

            if (key.Contains("sitsleepup") || key.Contains("supine") || key.Contains("仰向け"))
            {
                return PoseGroupKind.Supine;
            }

            if (key.Contains("sitsleepdown") || key.Contains("prone") || key.Contains("うつ伏せ"))
            {
                return PoseGroupKind.Prone;
            }

            return PoseGroupKind.Custom;
        }

        public static PoseGroupKind MapOverrideKind(string stateType)
        {
            var value = (stateType ?? "").ToLowerInvariant();
            if (value.Contains("stand"))
            {
                return PoseGroupKind.Standing;
            }

            if (value.Contains("crouch") || value.Contains("sit"))
            {
                return PoseGroupKind.Floor;
            }

            if (value.Contains("prone"))
            {
                return PoseGroupKind.Prone;
            }

            return PoseGroupKind.Custom;
        }

        public static TrackingPolicyData DefaultTracking(PoseGroupKind kind)
        {
            return TrackingPolicyUtility.DefaultForGroup(kind);
        }

        public static string DisplayName(KawaiiAnimationDto animation)
        {
            if (!string.IsNullOrWhiteSpace(animation.DisplayName))
            {
                return animation.DisplayName.Trim();
            }

            if (animation.Motion != null && !string.IsNullOrWhiteSpace(animation.Motion.name))
            {
                return ObjectNames.NicifyVariableName(animation.Motion.name);
            }

            return "Pose " + animation.Index;
        }

        public static IReadOnlyList<KawaiiAnimationDto> ImportableAnimations(
            KawaiiLayerDto layer,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report)
        {
            var result = new List<KawaiiAnimationDto>();
            foreach (var animation in layer.Animations.OrderBy(a => a.Index))
            {
                if (!animation.Enabled && !options.preserveDisabledPosesAsDisabled)
                {
                    report.Warning(
                        PoseTuneDiagnostics.KawaiiDisabledPoseSkipped.Code,
                        "disabled pose を skip しました: " + DisplayName(animation),
                        animation.Motion);
                    continue;
                }

                result.AddRange(KawaiiBlendTreeCompatibilityConverter.ExpandAnimation(animation, options));
            }

            return result;
        }
    }
}
