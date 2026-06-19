using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class ImportCandidateScoringContext
    {
        public AnimationClip Clip;
        public string StatePath = "";
        public string SourceLayerName = "";
        public bool HasHumanoidCurves;
        public bool HasTrackingBehavior;
        public List<List<ParameterConditionData>> ConditionBranches = new();
        public bool FromBlendTree;
    }

    internal sealed class ImportConfidenceResult
    {
        public float Score;
        public List<string> Reasons = new();
    }

    internal sealed class ImportConfidenceScorer
    {
        private static readonly string[] PoseTokens =
        {
            "pose", "sit", "chair", "floor", "prone", "supine", "lay", "lie", "idle", "stand", "kneel", "crouch"
        };

        public ImportConfidenceResult Score(ImportCandidateScoringContext context)
        {
            context ??= new ImportCandidateScoringContext();
            var result = new ImportConfidenceResult();
            AddIf(result, HasPoseToken(context.Clip != null ? context.Clip.name : ""), 0.25f, "clip name contains pose token");
            AddIf(result, HasPoseToken(context.StatePath), 0.20f, "state path contains pose token");
            AddIf(result, context.HasHumanoidCurves, 0.20f, "clip has humanoid/root curves");
            AddIf(result, context.HasTrackingBehavior, 0.15f, "state has tracking control");
            AddIf(result, context.ConditionBranches != null && context.ConditionBranches.Count > 0, 0.10f, "incoming conditions found");
            AddIf(result, IsLayerContextLikely(context.SourceLayerName), 0.05f, "source layer context is supported");
            AddIf(result, context.FromBlendTree, 0.05f, "blend tree metadata found");
            result.Score = Mathf.Clamp01(result.Score);
            if (result.Reasons.Count == 0)
            {
                result.Reasons.Add("no strong pose signal");
            }

            return result;
        }

        private static void AddIf(ImportConfidenceResult result, bool condition, float score, string reason)
        {
            if (!condition)
            {
                return;
            }

            result.Score += score;
            result.Reasons.Add(reason);
        }

        private static bool HasPoseToken(string text)
        {
            return PoseTokens.Any(token => (text ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsLayerContextLikely(string layerName)
        {
            return string.IsNullOrWhiteSpace(layerName) ||
                   layerName.IndexOf("Base", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   layerName.IndexOf("Action", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   layerName.IndexOf("Gesture", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
