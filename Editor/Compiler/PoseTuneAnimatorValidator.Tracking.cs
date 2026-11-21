using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class PoseTuneAnimatorValidator
    {
        private static readonly TrackingPart[] Parts = (TrackingPart[])Enum.GetValues(typeof(TrackingPart));

        private static readonly IReadOnlyDictionary<string, TrackingMode> ArbiterStateModes =
            new Dictionary<string, TrackingMode>
            {
                { PoseTuneNames.ArbiterStates.Idle, TrackingMode.NoChange },
                { PoseTuneNames.ArbiterStates.VoteAnimation, TrackingMode.Animation },
                { PoseTuneNames.ArbiterStates.VoteTracking, TrackingMode.Tracking },
                { PoseTuneNames.ArbiterStates.LockedAnimation, TrackingMode.Animation },
                { PoseTuneNames.ArbiterStates.RequestAnimation, TrackingMode.Animation },
                { PoseTuneNames.ArbiterStates.RequestTracking, TrackingMode.Tracking },
                { PoseTuneNames.ArbiterStates.RequestReset, TrackingMode.Tracking },
                { PoseTuneNames.ArbiterStates.LockRelease, TrackingMode.Tracking }
            };

        private static readonly string[] HeldArbiterStateNames =
        {
            PoseTuneNames.ArbiterStates.VoteAnimation,
            PoseTuneNames.ArbiterStates.VoteTracking,
            PoseTuneNames.ArbiterStates.LockedAnimation
        };

        private static void ValidateTrackingArbiters(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            foreach (var part in TrackingArbiterCompiler.RequiredParts(graph))
            {
                var layerName = PoseTuneNames.TrackingArbiterLayerName(part);
                var layer = FindLayer(controller, layerName);
                if (layer?.stateMachine == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        "Tracking arbiter layer が見つかりません: " + layerName,
                        graph.RootComponent);
                    continue;
                }

                foreach (var expected in ArbiterStateModes)
                {
                    var state = FindState(layer, expected.Key);
                    if (state == null)
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                            $"Tracking arbiter state が見つかりません: {layerName}/{expected.Key}",
                            graph.RootComponent);
                        continue;
                    }

                    ValidateTrackingBehavior(state, part, expected.Value, graph.RootComponent, report);
                    if (expected.Key.StartsWith("Request", StringComparison.Ordinal))
                    {
                        ValidateResetClearDriver(state, part, graph.RootComponent, report);
                    }
                }

                foreach (var transition in layer.stateMachine.anyStateTransitions ??
                             Array.Empty<TransitionView>())
                {
                    if (transition != null && transition.canTransitionToSelf)
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                            "Tracking arbiter の AnyState transition が自己遷移を許可しています: " + layerName,
                            graph.RootComponent);
                    }
                }

                ValidateArbiterTransitions(graph, layer, part, report);
                foreach (var heldStateName in HeldArbiterStateNames)
                {
                    ValidateRefreshLoop(FindState(layer, heldStateName), layerName, graph.RootComponent, report);
                }
            }
        }

        private static void ValidateTrackingVoteDefinitions(PoseGraph graph, ValidationReport report)
        {
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null && ParameterAllocator.RequiresTrackingVote(graph, group)))
            {
                // Group-wide duplicate names (not per layer bucket), so these variants are not shared with
                // ValidateGroupLayer.
                var duplicateStateBaseNames =
                    PoseStateNaming.DuplicateBaseNames(group.Poses.Where(candidate => candidate != null));
                var expected = group.Poses
                    .Where(pose => pose != null)
                    .SelectMany(pose => ExpectedVariants(graph, group, pose, duplicateStateBaseNames))
                    .Select(variant => variant.Policy)
                    .Where(policy => !TrackingPolicyUtility.IsNoChange(policy))
                    .Aggregate(
                        new List<TrackingPolicyData>(),
                        (profiles, policy) =>
                        {
                            if (!profiles.Any(existing => TrackingPolicyUtility.AreEqual(existing, policy)))
                            {
                                profiles.Add(policy);
                            }

                            return profiles;
                        })
                    .ToList();
                var actual = graph.TrackingVotes.Votes
                    .Where(vote => vote.GroupId == (group.Id ?? ""))
                    .ToList();
                UnityEngine.Object context = group.Source != null ? group.Source : graph.RootComponent;

                if (actual.Count != expected.Count)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        $"Tracking vote 定義数が distinct profile 数と一致しません: {group.DisplayName} " +
                        $"(actual={actual.Count}, expected={expected.Count})",
                        context);
                }

                if (actual.Select(vote => vote.Id).Distinct().Count() != actual.Count ||
                    actual.Any(vote => vote.Id <= 0))
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        "Tracking vote は group 内の各 distinct profile に固有の正値である必要があります: " + group.DisplayName,
                        context);
                }

                if (actual.Select(vote => vote.Policy)
                    .Where(policy => policy != null)
                    .GroupBy(policy => actual.Count(candidate =>
                        TrackingPolicyUtility.AreEqual(candidate.Policy, policy)))
                    .Any(grouping => grouping.Key != 1))
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        "Tracking vote の policy profile が重複しています: " + group.DisplayName,
                        context);
                }

                foreach (var policy in expected)
                {
                    if (actual.Count(candidate => TrackingPolicyUtility.AreEqual(candidate.Policy, policy)) != 1)
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                            $"Tracking vote の policy 対応が不正です: {group.DisplayName}",
                            graph.RootComponent);
                    }
                }
            }
        }

        private static void ValidateArbiterTransitions(
            PoseGraph graph,
            LayerView layer,
            TrackingPart part,
            ValidationReport report)
        {
            var votes = ExpectedArbiterVotes(graph, part);
            var animationVotes = votes.Where(vote => vote.Mode == TrackingMode.Animation).ToList();
            var trackingVotes = votes.Where(vote => vote.Mode == TrackingMode.Tracking).ToList();
            var resetParameter = PoseTuneNames.TrackingResetParameter(part);
            var lockParameter = PoseTuneNames.TrackingArbiterLockParameter(graph, part);
            var activePoseParameters = graph.HasPoseOptions
                ? PoseTuneLayerNaming.GroupActiveParameters(graph).ToList()
                : new List<string>();
            var anyTransitions = layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>();

            foreach (var vote in animationVotes)
            {
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.VoteAnimation,
                    transition => HasVoteCondition(transition, vote) && HasLockDisabledCondition(transition, lockParameter),
                    layer,
                    graph,
                    report);
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.RequestAnimation,
                    transition => HasVoteCondition(transition, vote) &&
                                  HasCondition(transition, resetParameter, AnimatorConditionMode.If, 0f) &&
                                  HasLockDisabledCondition(transition, lockParameter),
                    layer,
                    graph,
                    report);
            }

            foreach (var vote in trackingVotes)
            {
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.VoteTracking,
                    transition => HasVoteCondition(transition, vote) &&
                                  HasLockDisabledCondition(transition, lockParameter) &&
                                  HasAllVoteExclusions(transition, animationVotes),
                    layer,
                    graph,
                    report);
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.RequestTracking,
                    transition => HasVoteCondition(transition, vote) &&
                                  HasCondition(transition, resetParameter, AnimatorConditionMode.If, 0f) &&
                                  HasLockDisabledCondition(transition, lockParameter) &&
                                  HasAllVoteExclusions(transition, animationVotes),
                    layer,
                    graph,
                    report);
            }

            RequireAnyTransition(
                anyTransitions,
                PoseTuneNames.ArbiterStates.RequestReset,
                transition => HasCondition(transition, resetParameter, AnimatorConditionMode.If, 0f) &&
                              HasAllVoteExclusions(transition, animationVotes) &&
                              HasAllVoteExclusions(transition, trackingVotes),
                layer,
                graph,
                report);

            ValidateNoWinnerReturn(layer, PoseTuneNames.ArbiterStates.VoteAnimation, animationVotes, trackingVotes, graph, report);
            ValidateNoWinnerReturn(layer, PoseTuneNames.ArbiterStates.VoteTracking, animationVotes, trackingVotes, graph, report);

            if (string.IsNullOrWhiteSpace(lockParameter))
            {
                return;
            }

            foreach (var activePoseParameter in activePoseParameters)
            {
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.LockedAnimation,
                    transition => HasCondition(
                                      transition,
                                      activePoseParameter,
                                      AnimatorConditionMode.Greater,
                                      0.5f) &&
                                  HasCondition(transition, lockParameter, AnimatorConditionMode.If, 0f),
                    layer,
                    graph,
                    report);
                RequireAnyTransition(
                    anyTransitions,
                    PoseTuneNames.ArbiterStates.RequestAnimation,
                    transition => HasCondition(
                                      transition,
                                      activePoseParameter,
                                      AnimatorConditionMode.Greater,
                                      0.5f) &&
                                  HasCondition(transition, resetParameter, AnimatorConditionMode.If, 0f) &&
                                  HasCondition(transition, lockParameter, AnimatorConditionMode.If, 0f),
                    layer,
                    graph,
                    report);
            }

            var locked = FindState(layer, PoseTuneNames.ArbiterStates.LockedAnimation);
            var lockRelease = FindState(layer, PoseTuneNames.ArbiterStates.LockRelease);
            if (locked == null || lockRelease == null ||
                !(locked.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                    transition != null &&
                    transition.destinationState == lockRelease &&
                    HasCondition(transition, lockParameter, AnimatorConditionMode.IfNot, 0f) &&
                    HasAllVoteExclusions(transition, animationVotes) &&
                    HasAllVoteExclusions(transition, trackingVotes)))
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    "Lock OFF 時に winner 不在を Tracking へ戻す遷移がありません: " + layer.name,
                    graph.RootComponent);
            }
        }

        private static void ValidateNoWinnerReturn(
            LayerView layer,
            string stateName,
            IReadOnlyList<ExpectedArbiterVote> animationVotes,
            IReadOnlyList<ExpectedArbiterVote> trackingVotes,
            PoseGraph graph,
            ValidationReport report)
        {
            var state = FindState(layer, stateName);
            var idle = FindState(layer, PoseTuneNames.ArbiterStates.Idle);
            if (state != null && idle != null &&
                (state.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                    transition != null &&
                    transition.destinationState == idle &&
                    HasAllVoteExclusions(transition, animationVotes) &&
                    HasAllVoteExclusions(transition, trackingVotes)))
            {
                return;
            }

            report.Error(
                PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                $"Tracking winner 不在時に Idle へ戻る遷移がありません: {layer.name}/{stateName}",
                graph.RootComponent);
        }

        private static void RequireAnyTransition(
            IEnumerable<TransitionView> transitions,
            string destinationName,
            Func<TransitionView, bool> predicate,
            LayerView layer,
            PoseGraph graph,
            ValidationReport report)
        {
            if (transitions.Any(transition =>
                    transition != null &&
                    transition.destinationState != null &&
                    transition.destinationState.name == destinationName &&
                    predicate(transition)))
            {
                return;
            }

            report.Error(
                PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                $"Tracking arbiter transition が不足または条件不正です: {layer.name} -> {destinationName}",
                graph.RootComponent);
        }

        private static List<ExpectedArbiterVote> ExpectedArbiterVotes(PoseGraph graph, TrackingPart part)
        {
            var groups = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => group != null)
                .GroupBy(group => group.Id ?? "")
                .ToDictionary(group => group.Key, group => group.First());
            return graph.TrackingVotes.Votes
                .Where(vote => groups.ContainsKey(vote.GroupId ?? ""))
                .Select(vote => new ExpectedArbiterVote
                {
                    Parameter = PoseTuneNames.TrackingVoteParameter(groups[vote.GroupId ?? ""]),
                    VoteId = vote.Id,
                    Mode = TrackingPolicyUtility.Get(vote.Policy, part)
                })
                .ToList();
        }

        private static bool HasVoteCondition(
            TransitionView transition,
            ExpectedArbiterVote vote)
        {
            return HasCondition(
                transition,
                vote.Parameter,
                AnimatorConditionMode.Equals,
                vote.VoteId);
        }

        private static bool HasAllVoteExclusions(
            TransitionView transition,
            IEnumerable<ExpectedArbiterVote> votes)
        {
            return votes.All(vote => HasCondition(
                transition,
                vote.Parameter,
                AnimatorConditionMode.NotEqual,
                vote.VoteId));
        }

        private static bool HasLockDisabledCondition(
            TransitionView transition,
            string lockParameter)
        {
            return string.IsNullOrWhiteSpace(lockParameter) ||
                   HasCondition(transition, lockParameter, AnimatorConditionMode.IfNot, 0f);
        }

        private static void ValidateTrackingBehavior(
            StateView state,
            TrackingPart expectedPart,
            TrackingMode expectedMode,
            UnityEngine.Object context,
            ValidationReport report)
        {
            var behaviours = (state.behaviours ?? Array.Empty<StateMachineBehaviour>())
                .OfType<VRCAnimatorTrackingControl>()
                .ToList();
            if (behaviours.Count != 1)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Tracking arbiter state の TrackingControl 数が不正です: {state.name} ({behaviours.Count})",
                    context);
                return;
            }

            var behaviour = behaviours[0];
            foreach (var part in Parts)
            {
                var actual = TrackingModeOf(behaviour, part);
                var expected = part == expectedPart ? expectedMode : TrackingMode.NoChange;
                if (actual == expected)
                {
                    continue;
                }

                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"TrackingControl が担当外部位を変更するか、mode が不正です: {state.name}/{part} ({actual}, expected {expected})",
                    context);
            }
        }

        private static TrackingMode TrackingModeOf(VRCAnimatorTrackingControl behaviour, TrackingPart part)
        {
            switch (part)
            {
                case TrackingPart.Head:
                    return FromVrc(behaviour.trackingHead);
                case TrackingPart.LeftHand:
                    return FromVrc(behaviour.trackingLeftHand);
                case TrackingPart.RightHand:
                    return FromVrc(behaviour.trackingRightHand);
                case TrackingPart.Hip:
                    return FromVrc(behaviour.trackingHip);
                case TrackingPart.LeftFoot:
                    return FromVrc(behaviour.trackingLeftFoot);
                case TrackingPart.RightFoot:
                    return FromVrc(behaviour.trackingRightFoot);
                case TrackingPart.LeftFingers:
                    return FromVrc(behaviour.trackingLeftFingers);
                case TrackingPart.RightFingers:
                    return FromVrc(behaviour.trackingRightFingers);
                case TrackingPart.Eyes:
                    return FromVrc(behaviour.trackingEyes);
                case TrackingPart.Mouth:
                    return FromVrc(behaviour.trackingMouth);
                default:
                    return TrackingMode.NoChange;
            }
        }

        private static TrackingMode FromVrc(VRC_AnimatorTrackingControl.TrackingType mode)
        {
            switch (mode)
            {
                case VRC_AnimatorTrackingControl.TrackingType.Animation:
                    return TrackingMode.Animation;
                case VRC_AnimatorTrackingControl.TrackingType.Tracking:
                    return TrackingMode.Tracking;
                default:
                    return TrackingMode.NoChange;
            }
        }

        private static void ValidateResetClearDriver(
            StateView state,
            TrackingPart part,
            UnityEngine.Object context,
            ValidationReport report)
        {
            var resetName = PoseTuneNames.TrackingResetParameter(part);
            var clearsReset = ParametersSetBy(state)
                .Any(parameter => parameter.name == resetName && Mathf.Approximately(parameter.value, 0f));
            if (!clearsReset)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Tracking reset request を消費する ParameterDriver がありません: {state.name}/{resetName}",
                    context);
            }
        }
    }
}
