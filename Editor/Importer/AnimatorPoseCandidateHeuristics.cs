using System;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorPoseCandidateHeuristics
    {
        private static readonly string[] PoseTokens =
        {
            "pose", "sit", "chair", "floor", "prone", "supine", "lay", "lie", "idle", "stand", "kneel", "crouch"
        };

        public static bool LooksLikePose(string text)
        {
            return PoseTokens.Any(token => text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool HasHumanoidCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            return bindings.Any(binding =>
                binding.type == typeof(Animator)
                || binding.propertyName.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0
                || binding.propertyName.IndexOf("Muscle", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static PoseGroupKind GuessGroupKind(string text)
        {
            if (ContainsAny(text, "chair", "sit", "seated"))
            {
                return PoseGroupKind.Chair;
            }

            if (ContainsAny(text, "floor", "kneel", "crouch"))
            {
                return PoseGroupKind.Floor;
            }

            if (ContainsAny(text, "prone", "crawl", "belly"))
            {
                return PoseGroupKind.Prone;
            }

            if (ContainsAny(text, "supine", "back", "sleep"))
            {
                return PoseGroupKind.Supine;
            }

            return PoseGroupKind.Standing;
        }

        public static bool IsActionLayerName(string layerName)
        {
            return !string.IsNullOrWhiteSpace(layerName) &&
                   layerName.IndexOf("Action", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsGroupKindEnabledByOptions(PoseGroupKind kind, ImportAnalysisOptions options)
        {
            switch (kind)
            {
                case PoseGroupKind.Floor:
                    return options.importCrouch;
                case PoseGroupKind.Prone:
                case PoseGroupKind.Supine:
                    return options.importProne;
                case PoseGroupKind.Standing:
                    return options.importStand;
                default:
                    return true;
            }
        }

        public static string DisabledReasonFor(PoseGroupKind kind)
        {
            switch (kind)
            {
                case PoseGroupKind.Floor:
                    return "importCrouch is false";
                case PoseGroupKind.Prone:
                case PoseGroupKind.Supine:
                    return "importProne is false";
                case PoseGroupKind.Standing:
                    return "importStand is false";
                default:
                    return "";
            }
        }

        private static bool ContainsAny(string text, params string[] tokens)
        {
            return tokens.Any(token => text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
