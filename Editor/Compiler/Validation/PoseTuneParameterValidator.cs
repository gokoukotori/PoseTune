using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneParameterValidator
    {
        private static readonly HashSet<string> VrchatBuiltinParameters = new()
        {
            "IsLocal",
            "PreviewMode",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "VRCEmote",
            "VRCFaceBlendH",
            "VRCFaceBlendV",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Earmuffs",
            "IsOnFriendsList",
            "AvatarVersion",
            "ScaleFactor",
            "ScaleFactorInverse",
            "ScaleModified",
            "EyeHeightAsMeters",
            "EyeHeightAsPercent",
            "VelocityMagnitude",
            "IsAnimatorEnabled"
        };

        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            var seen = new Dictionary<string, PoseTuneParameterSyncType>();
            var reserved = ReservedParameterNames(graph);
            if (graph.HasGoroneSystemExGuard)
            {
                Check(GoroneSystemExDetector.VrcSupineParameter, PoseTuneParameterSyncType.Int,
                    graph.GoroneSystemExCompatibility);
            }

            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => PoseTuneCompilerRules.RequiresPoseSelectionParameter(graph.RootComponent, group)))
            {
                Check(group.ParameterName, PoseTuneParameterSyncType.Int, group.Source);
            }

            if (ParameterAllocator.NeedsGeneratedHeightParameter(graph))
            {
                Check(PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust),
                    PoseTuneParameterSyncType.Float, graph.HeightAdjust);
            }

            foreach (var pose in graph.Poses)
            {
                if (pose.MotionTime != null && pose.MotionTime.mode == MotionTimeMode.UseGeneratedHeightParameter)
                {
                    continue;
                }

                var motionTimeParameter = MotionTimeParameterResolver.Resolve(
                    graph,
                    pose,
                    MotionTimeParameterUsage.ParameterValidation);
                if (motionTimeParameter.HasParameter)
                {
                    Check(motionTimeParameter.ParameterName, PoseTuneParameterSyncType.Float, pose.Source);
                }
            }

            foreach (var pose in graph.Poses)
            {
                ValidateBlendTreeParameters(pose, reserved, seen, report);
                ValidateConditions(pose.Conditions, pose.Source, report);
                foreach (var branch in pose.ConditionBranches ?? new List<List<ParameterConditionData>>())
                {
                    ValidateConditions(branch, pose.Source, report);
                }
            }

            foreach (var group in graph.Groups)
            {
                ValidateConditions(group.Conditions, group.Source, report);
            }

            void Check(string name, PoseTuneParameterSyncType type, Object context)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameEmpty.Code, "パラメータ名が空です。", context);
                    return;
                }

                if (name.Split('/').Any(string.IsNullOrWhiteSpace))
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameEmpty.Code, "パラメータ名に空のセグメントがあります: " + name, context);
                    return;
                }

                if (reserved.Contains(name) || name.StartsWith("PTI/"))
                {
                    report.Error(PoseTuneDiagnostics.ParameterReservedName.Code, "予約済みのパラメータ名です: " + name, context);
                }

                if (seen.TryGetValue(name, out var previous))
                {
                    var message = previous != type
                        ? "重複したパラメータ名の型が異なります: "
                        : "パラメータ名が重複しています: ";
                    report.Error(PoseTuneDiagnostics.ParameterNameConflict.Code, message + name, context);
                }
                else
                {
                    seen[name] = type;
                }
            }
        }

        public static void Validate(PoseTuneValidationContext context, ValidationReport report)
        {
            Validate(context.Graph, report);
            PoseTuneAvatarParameterValidator.Validate(context, report);
        }

        private static HashSet<string> ReservedParameterNames(PoseGraph graph)
        {
            var reserved = new HashSet<string>(VrchatBuiltinParameters);
            if (graph.RootComponent == null)
            {
                return reserved;
            }

            reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.Mode));
            reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.SupineFlag));
            if (graph.HasPoseOptions)
            {
                reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.LockHead));
                reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.LockHands));
                reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.LockFeet));
                reserved.Add(graph.RootComponent.Parameter(PoseTuneNames.LocomotionLock));
            }

            return reserved;
        }

        private static void ValidateBlendTreeParameters(
            PoseDefinition pose,
            HashSet<string> reserved,
            Dictionary<string, PoseTuneParameterSyncType> seen,
            ValidationReport report)
        {
            foreach (var reference in BlendTreeParameterCollector.CollectParameterReferences(pose?.SourceMotion))
            {
                var name = reference.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameEmpty.Code, "BlendTree parameter 名が空です。", pose?.Source);
                    continue;
                }

                if (name.Split('/').Any(string.IsNullOrWhiteSpace))
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameEmpty.Code, "BlendTree parameter 名に空のセグメントがあります: " + name, pose?.Source);
                    continue;
                }

                if (reserved.Contains(name) || name.StartsWith("PTI/"))
                {
                    report.Error(PoseTuneDiagnostics.ParameterReservedName.Code, "予約済みの BlendTree parameter 名です: " + name, pose?.Source);
                    continue;
                }

                if (seen.TryGetValue(name, out var previous) && previous != PoseTuneParameterSyncType.Float)
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameConflict.Code,
                        "BlendTree parameter は Float が必要ですが、同名の生成 parameter 型が異なります: " + name,
                        pose?.Source);
                }
                else if (!seen.ContainsKey(name))
                {
                    seen[name] = PoseTuneParameterSyncType.Float;
                }
            }
        }

        private static void ValidateConditions(
            IEnumerable<ParameterConditionData> conditions,
            Object context,
            ValidationReport report)
        {
            foreach (var condition in conditions ?? Enumerable.Empty<ParameterConditionData>())
            {
                if (condition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(condition.parameter))
                {
                    report.Error(PoseTuneDiagnostics.ParameterNameEmpty.Code, "条件 parameter が空です。", context);
                }
            }
        }

    }
}
