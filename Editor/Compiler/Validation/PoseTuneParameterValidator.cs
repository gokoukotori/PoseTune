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
                         .Where(group => PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group)))
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

    }
}
