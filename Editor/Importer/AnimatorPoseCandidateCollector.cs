using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Conditions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorPoseCandidateCollector
    {
        public static int CountPoseCandidates(AnimatorStateMachine stateMachine, string path, HashSet<string> seen)
        {
            if (stateMachine == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var child in stateMachine.states)
            {
                count += CountPoseCandidates(child.state.motion, path + "/" + child.state.name, seen);
            }

            foreach (var child in stateMachine.stateMachines)
            {
                count += CountPoseCandidates(child.stateMachine, path + "/" + child.stateMachine.name, seen);
            }

            return count;
        }

        public static void TraverseStateMachine(
            AnimatorStateMachine stateMachine,
            string path,
            int sourceLayerIndex,
            string sourceLayerName,
            List<ImportCandidate> result,
            HashSet<string> seen,
            ImportAnalysisOptions options)
        {
            if (stateMachine == null)
            {
                return;
            }

            foreach (var child in stateMachine.states)
            {
                var statePath = path + "/" + child.state.name;
                CollectMotion(child.state.motion, child.state, stateMachine, statePath, sourceLayerIndex, sourceLayerName, result,
                    seen, options, new List<BlendTreeChildInfo>());
            }

            foreach (var child in stateMachine.stateMachines)
            {
                TraverseStateMachine(child.stateMachine, path + "/" + child.stateMachine.name, sourceLayerIndex, sourceLayerName,
                    result, seen, options);
            }
        }

        private static int CountPoseCandidates(Motion motion, string path, HashSet<string> seen)
        {
            switch (motion)
            {
                case null:
                    return 0;
                case AnimationClip clip:
                    var seenKey = clip.GetInstanceID() + ":" + path;
                    return seen.Add(seenKey) &&
                           (AnimatorPoseCandidateHeuristics.LooksLikePose(clip.name ?? "") ||
                            AnimatorPoseCandidateHeuristics.HasHumanoidCurves(clip))
                        ? 1
                        : 0;
                case BlendTree tree:
                    return tree.children.Sum(child => CountPoseCandidates(child.motion, path + "/" + tree.name, seen));
                default:
                    return 0;
            }
        }

        private static void CollectMotion(
            Motion motion,
            AnimatorState state,
            AnimatorStateMachine stateMachine,
            string path,
            int sourceLayerIndex,
            string sourceLayerName,
            List<ImportCandidate> result,
            HashSet<string> seen,
            ImportAnalysisOptions options,
            List<BlendTreeChildInfo> blendTreePath)
        {
            switch (motion)
            {
                case null:
                    return;
                case AnimationClip clip:
                    AddClip(clip, state, stateMachine, path, sourceLayerIndex, sourceLayerName, result, seen, options,
                        blendTreePath);
                    return;
                case BlendTree tree:
                    foreach (var child in tree.children)
                    {
                        var nextPath = new List<BlendTreeChildInfo>(blendTreePath)
                        {
                            new()
                            {
                                BlendTreeName = tree.name ?? "",
                                BlendParameter = tree.blendParameter ?? "",
                                Threshold = child.threshold,
                                Position = child.position,
                                TimeScale = child.timeScale,
                                ChildMotionName = child.motion != null ? child.motion.name : ""
                            }
                        };
                        CollectMotion(child.motion, state, stateMachine, path + "/" + tree.name, sourceLayerIndex, sourceLayerName,
                            result, seen, options, nextPath);
                    }

                    break;
            }
        }

        private static void AddClip(
            AnimationClip clip,
            AnimatorState state,
            AnimatorStateMachine stateMachine,
            string path,
            int sourceLayerIndex,
            string sourceLayerName,
            List<ImportCandidate> result,
            HashSet<string> seen,
            ImportAnalysisOptions options,
            List<BlendTreeChildInfo> blendTreePath)
        {
            if (clip == null)
            {
                return;
            }

            var seenKey = clip.GetInstanceID() + ":" + path;
            if (!seen.Add(seenKey))
            {
                return;
            }

            var name = clip.name ?? "";
            if (!AnimatorPoseCandidateHeuristics.LooksLikePose(name) &&
                !AnimatorPoseCandidateHeuristics.HasHumanoidCurves(clip))
            {
                return;
            }

            var conditionBranches = AnimatorConditionBranchReader.ReadIncomingConditionBranches(stateMachine, state);
            var groupKind = AnimatorPoseCandidateHeuristics.GuessGroupKind(name + " " + path);
            var enabled = AnimatorPoseCandidateHeuristics.IsGroupKindEnabledByOptions(groupKind, options);
            if (!enabled && !options.createDisabledCandidates)
            {
                return;
            }

            var confidence = new ImportConfidenceScorer().Score(new ImportCandidateScoringContext
            {
                Clip = clip,
                StatePath = path,
                SourceLayerName = sourceLayerName ?? "",
                HasHumanoidCurves = AnimatorPoseCandidateHeuristics.HasHumanoidCurves(clip),
                HasTrackingBehavior = AnimatorTrackingPolicyReader.HasTrackingBehavior(state),
                ConditionBranches = conditionBranches,
                FromBlendTree = blendTreePath != null && blendTreePath.Count > 0
            });
            var selectedByConfidence = confidence.Score >= Mathf.Clamp01(options.minConfidenceForDefaultSelection);
            result.Add(new ImportCandidate
            {
                Clip = clip,
                DisplayName = ObjectNames.NicifyVariableName(name),
                AnimatorPath = path,
                SourceLayerIndex = sourceLayerIndex,
                SourceLayerName = sourceLayerName ?? "",
                StateName = state != null ? state.name : "",
                StatePath = path,
                MotionPath = path,
                GroupKind = groupKind,
                Target = options.target,
                EnabledByDefault = enabled && selectedByConfidence,
                Confidence = confidence.Score,
                ConfidenceReasons = confidence.Reasons,
                DisabledReason = enabled ? "" : AnimatorPoseCandidateHeuristics.DisabledReasonFor(groupKind),
                FromBlendTree = blendTreePath != null && blendTreePath.Count > 0,
                BlendTreePath = CopyBlendTreePath(blendTreePath),
                TrackingPolicy = AnimatorTrackingPolicyReader.ReadTrackingPolicy(state),
                Conditions = AnimatorConditionBranchReader.FlattenConditionBranches(conditionBranches),
                ConditionBranches = conditionBranches,
                ConditionBranchInfos = conditionBranches.Select((branch, index) => new ImportConditionBranchInfo
                {
                    Source = "Incoming " + (index + 1),
                    Conditions = branch.Select(PoseTuneConditionUtility.Copy).ToList()
                }).ToList()
            });
        }

        private static List<BlendTreeChildInfo> CopyBlendTreePath(IEnumerable<BlendTreeChildInfo> path)
        {
            return (path ?? Enumerable.Empty<BlendTreeChildInfo>())
                .Select(info => new BlendTreeChildInfo
                {
                    BlendTreeName = info.BlendTreeName,
                    BlendParameter = info.BlendParameter,
                    Threshold = info.Threshold,
                    Position = info.Position,
                    TimeScale = info.TimeScale,
                    ChildMotionName = info.ChildMotionName
                })
                .ToList();
        }
    }
}
