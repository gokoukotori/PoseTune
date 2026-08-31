using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneAnimatorValidator
    {
        private static readonly TrackingPart[] Parts = (TrackingPart[])Enum.GetValues(typeof(TrackingPart));

        private static readonly IReadOnlyDictionary<string, TrackingMode> ArbiterStateModes =
            new Dictionary<string, TrackingMode>
            {
                { "Idle", TrackingMode.NoChange },
                { "VoteAnimation", TrackingMode.Animation },
                { "VoteTracking", TrackingMode.Tracking },
                { "LockedAnimation", TrackingMode.Animation },
                { "RequestAnimation", TrackingMode.Animation },
                { "RequestTracking", TrackingMode.Tracking },
                { "RequestReset", TrackingMode.Tracking },
                { "LockRelease", TrackingMode.Tracking }
            };

        public ValidationReport Validate(PoseGraph graph, AnimatorController controller)
        {
            return Validate(graph, controller, new ParameterAllocator().Allocate(graph));
        }

        public ValidationReport Validate(
            PoseGraph graph,
            AnimatorController controller,
            ParameterPlan parameters)
        {
            return Validate(graph, ControllerView.Create(controller), parameters);
        }

        public ValidationReport Validate(PoseGraph graph, VirtualAnimatorController controller)
        {
            return Validate(graph, controller, new ParameterAllocator().Allocate(graph));
        }

        public ValidationReport Validate(
            PoseGraph graph,
            VirtualAnimatorController controller,
            ParameterPlan parameters)
        {
            return Validate(graph, ControllerView.Create(controller), parameters);
        }

        private static ValidationReport Validate(
            PoseGraph graph,
            ControllerView controller,
            ParameterPlan parameters)
        {
            var report = new ValidationReport();
            if (graph?.RootComponent == null || controller == null)
            {
                return report;
            }

            ValidateTrackingParameters(graph, controller, report);
            ValidateTrackingVoteDefinitions(graph, report);
            ValidateTrackingArbiters(graph, controller, report);
            var poseSelection = parameters?.PoseSelection ?? PoseSelectionPlanner.Build(graph);
            ValidatePoseSelectionParameters(poseSelection, controller, graph, report);

            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph).Where(group => group != null))
            {
                foreach (var bucket in PoseTuneLayerNaming.LayerBuckets(group))
                {
                    ValidateGroupLayer(
                        graph,
                        controller,
                        group,
                        bucket.LayerName,
                        bucket.Poses?.Where(pose => pose != null).ToList() ?? new List<PoseDefinition>(),
                        poseSelection,
                        report);
                }

                ValidateAutoPreemption(graph, controller, group, poseSelection, report);
            }

            return report;
        }

        private static void ValidateTrackingParameters(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            var parameters = controller.parameters ?? Array.Empty<AnimatorControllerParameter>();
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null && ParameterAllocator.RequiresTrackingVote(graph, group)))
            {
                ValidateParameter(
                    parameters,
                    PoseTuneNames.TrackingVoteParameter(group),
                    AnimatorControllerParameterType.Int,
                    graph.RootComponent,
                    report);
            }

            foreach (var part in TrackingArbiterCompiler.RequiredParts(graph))
            {
                ValidateParameter(
                    parameters,
                    PoseTuneNames.TrackingResetParameter(part),
                    AnimatorControllerParameterType.Bool,
                    graph.RootComponent,
                    report);
            }

            foreach (var activeParameter in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null)
                         .SelectMany(PoseTuneLayerNaming.GroupActiveParameters)
                         .Distinct())
            {
                ValidateParameter(
                    parameters,
                    activeParameter,
                    AnimatorControllerParameterType.Float,
                    graph.RootComponent,
                    report);
            }

        }

        private static void ValidatePoseSelectionParameters(
            PoseSelectionPlan poseSelection,
            ControllerView controller,
            PoseGraph graph,
            ValidationReport report)
        {
            var parameters = controller.parameters ?? Array.Empty<AnimatorControllerParameter>();
            foreach (var channel in poseSelection.Channels)
            {
                var parameter = parameters.FirstOrDefault(candidate =>
                    candidate != null && candidate.name == channel.ParameterName);
                if (parameter != null && parameter.type == AnimatorControllerParameterType.Int)
                {
                    continue;
                }

                report.Error(
                    PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                    $"Pose selection parameter がないか型が不正です: {channel.ParameterName} (Int)",
                    graph.RootComponent);
            }
        }

        private static void ValidateParameter(
            IEnumerable<AnimatorControllerParameter> parameters,
            string name,
            AnimatorControllerParameterType expectedType,
            UnityEngine.Object context,
            ValidationReport report)
        {
            var parameter = parameters.FirstOrDefault(candidate => candidate != null && candidate.name == name);
            if (parameter == null || parameter.type != expectedType)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Tracking parameter がないか型が不正です: {name} ({expectedType})",
                    context);
            }
        }

        private static void ValidateTrackingArbiters(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            foreach (var part in TrackingArbiterCompiler.RequiredParts(graph))
            {
                var layerName = PoseTuneNames.TrackingArbiterLayerName(part);
                var layer = Layers(controller).FirstOrDefault(candidate => candidate != null && candidate.name == layerName);
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
            }
        }

        private static void ValidateTrackingVoteDefinitions(PoseGraph graph, ValidationReport report)
        {
            foreach (var group in PoseGraphBuildFilter.BuildableGroups(graph)
                         .Where(group => group != null && ParameterAllocator.RequiresTrackingVote(graph, group)))
            {
                var expected = group.Poses
                    .Where(pose => pose != null)
                    .SelectMany(pose => ExpectedVariants(
                        graph,
                        group,
                        pose,
                        PoseStateNaming.DuplicateBaseNames(group.Poses.Where(candidate => candidate != null))))
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

                if (actual.Count != expected.Count)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        $"Tracking vote 定義数が distinct profile 数と一致しません: {group.DisplayName} " +
                        $"(actual={actual.Count}, expected={expected.Count})",
                        group.Source != null ? group.Source : graph.RootComponent);
                }

                if (actual.Select(vote => vote.Id).Distinct().Count() != actual.Count ||
                    actual.Any(vote => vote.Id <= 0))
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        "Tracking vote は group 内の各 distinct profile に固有の正値である必要があります: " + group.DisplayName,
                        group.Source != null ? group.Source : graph.RootComponent);
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
                        group.Source != null ? group.Source : graph.RootComponent);
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
            var lockParameter = ArbiterLockParameter(graph, part);
            var activePoseParameters = graph.HasPoseOptions
                ? PoseTuneLayerNaming.GroupActiveParameters(graph).ToList()
                : new List<string>();
            var anyTransitions = layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>();

            foreach (var vote in animationVotes)
            {
                RequireAnyTransition(
                    anyTransitions,
                    "VoteAnimation",
                    transition => HasVoteCondition(transition, vote) && HasLockDisabledCondition(transition, lockParameter),
                    layer,
                    graph,
                    report);
                RequireAnyTransition(
                    anyTransitions,
                    "RequestAnimation",
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
                    "VoteTracking",
                    transition => HasVoteCondition(transition, vote) &&
                                  HasLockDisabledCondition(transition, lockParameter) &&
                                  HasAllVoteExclusions(transition, animationVotes),
                    layer,
                    graph,
                    report);
                RequireAnyTransition(
                    anyTransitions,
                    "RequestTracking",
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
                "RequestReset",
                transition => HasCondition(transition, resetParameter, AnimatorConditionMode.If, 0f) &&
                              HasAllVoteExclusions(transition, animationVotes) &&
                              HasAllVoteExclusions(transition, trackingVotes),
                layer,
                graph,
                report);

            ValidateNoWinnerReturn(layer, "VoteAnimation", animationVotes, trackingVotes, graph, report);
            ValidateNoWinnerReturn(layer, "VoteTracking", animationVotes, trackingVotes, graph, report);

            if (string.IsNullOrWhiteSpace(lockParameter))
            {
                return;
            }

            foreach (var activePoseParameter in activePoseParameters)
            {
                RequireAnyTransition(
                    anyTransitions,
                    "LockedAnimation",
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
                    "RequestAnimation",
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

            var locked = FindState(layer, "LockedAnimation");
            var lockRelease = FindState(layer, "LockRelease");
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
            var idle = FindState(layer, "Idle");
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
                    Mode = TrackingCompiler.Mode(vote.Policy, part)
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

        private static string ArbiterLockParameter(PoseGraph graph, TrackingPart part)
        {
            if (graph?.HasPoseOptions != true || graph.RootComponent == null)
            {
                return "";
            }

            switch (part)
            {
                case TrackingPart.Head:
                    return graph.RootComponent.Parameter(PoseTuneNames.LockHead);
                case TrackingPart.LeftHand:
                case TrackingPart.RightHand:
                case TrackingPart.LeftFingers:
                case TrackingPart.RightFingers:
                    return graph.RootComponent.Parameter(PoseTuneNames.LockHands);
                case TrackingPart.LeftFoot:
                case TrackingPart.RightFoot:
                    return graph.RootComponent.Parameter(PoseTuneNames.LockFeet);
                default:
                    return "";
            }
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

        private static void ValidateGroupLayer(
            PoseGraph graph,
            ControllerView controller,
            PoseGroupDefinition group,
            string layerName,
            List<PoseDefinition> poses,
            PoseSelectionPlan poseSelection,
            ValidationReport report)
        {
            var layer = Layers(controller).FirstOrDefault(candidate => candidate != null && candidate.name == layerName);
            if (layer?.stateMachine == null)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                    "Pose layer が見つかりません: " + layerName,
                    graph.RootComponent);
                return;
            }

            var duplicateStateBaseNames = PoseStateNaming.DuplicateBaseNames(poses);
            foreach (var pose in poses)
            {
                ValidatePoseHandoff(graph, group, layer, pose, duplicateStateBaseNames, report);
                ValidateFbtGuard(graph, layer, pose, duplicateStateBaseNames, report);
                ValidatePoseSelectionTransitions(
                    graph,
                    group,
                    layer,
                    pose,
                    duplicateStateBaseNames,
                    poseSelection,
                    report);
            }

            ValidateSharedExclusiveResetDrivers(
                graph,
                group,
                layer,
                poses,
                duplicateStateBaseNames,
                poseSelection,
                report);

            if (ParameterAllocator.RequiresTrackingVote(graph, group))
            {
                var voteName = PoseTuneNames.TrackingVoteParameter(group);
                foreach (var transition in layer.stateMachine.anyStateTransitions ??
                             Array.Empty<TransitionView>())
                {
                    if (transition != null && !HasCondition(
                            transition,
                            voteName,
                            AnimatorConditionMode.Equals,
                            0f))
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                            "Pose entry transition が tracking vote=0 を待っていません: " + layerName,
                            graph.RootComponent);
                    }
                }
            }

            foreach (var transition in layer.stateMachine.anyStateTransitions ??
                         Array.Empty<TransitionView>())
            {
                foreach (var activeParameter in PoseTuneLayerNaming.GroupActiveParameters(group))
                {
                    if (transition != null && !HasCondition(
                            transition,
                            activeParameter,
                            AnimatorConditionMode.Less,
                            0.5f))
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                            "Pose entry transition が group inactive を待っていません: " + layerName,
                            graph.RootComponent);
                    }
                }
            }
        }

        private static void ValidatePoseSelectionTransitions(
            PoseGraph graph,
            PoseGroupDefinition group,
            LayerView layer,
            PoseDefinition pose,
            HashSet<string> duplicateStateBaseNames,
            PoseSelectionPlan poseSelection,
            ValidationReport report)
        {
            if (!PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
            {
                return;
            }

            var binding = poseSelection.Find(pose);
            if (binding == null)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                    "Pose selection binding がありません: " + pose.DisplayName,
                    pose.Source);
                return;
            }

            var modeParameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var anyTransitions = layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>();
            foreach (var variant in ExpectedVariants(graph, group, pose, duplicateStateBaseNames))
            {
                var hasEntry = anyTransitions.Any(transition =>
                    transition?.destinationState != null &&
                    (transition.destinationState.name == variant.StateName ||
                     transition.destinationState.name == "CommitExclusive_" + variant.StateName) &&
                    HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 2f) &&
                    HasCondition(
                        transition,
                        binding.ParameterName,
                        AnimatorConditionMode.Equals,
                        binding.Value));
                if (!hasEntry)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                        $"Manual pose entry が共有selection条件を使用していません: {variant.StateName} ({binding.ParameterName}={binding.Value})",
                        pose.Source);
                }

                var state = FindState(layer, variant.StateName);
                var handoff = FindState(layer, variant.HandoffName);
                var hasExit = state != null && handoff != null &&
                              (state.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                                  transition?.destinationState == handoff &&
                                  HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 2f) &&
                                  HasCondition(
                                      transition,
                                      binding.ParameterName,
                                      AnimatorConditionMode.NotEqual,
                                      binding.Value));
                if (!hasExit)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                        $"Manual pose exit が共有selection条件を使用していません: {variant.StateName} ({binding.ParameterName}!={binding.Value})",
                        pose.Source);
                }
            }
        }

        private static void ValidateSharedExclusiveResetDrivers(
            PoseGraph graph,
            PoseGroupDefinition group,
            LayerView layer,
            IEnumerable<PoseDefinition> poses,
            HashSet<string> duplicateStateBaseNames,
            PoseSelectionPlan poseSelection,
            ValidationReport report)
        {
            if (graph.RootComponent.poseSelectionSyncMode != PoseSelectionSyncMode.SharedExclusivePoseId ||
                !group.Exclusive ||
                !PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
            {
                return;
            }

            var resetNames = poseSelection.ExclusiveResetParameterNames(
                graph.RootComponent,
                PoseGraphBuildFilter.BuildableGroups(graph),
                group);
            var expected = new HashSet<string>(resetNames, StringComparer.Ordinal);
            var currentParameter = poseSelection.Find(group)?.ParameterName ?? "";
            foreach (var pose in poses)
            {
                foreach (var variant in ExpectedVariants(graph, group, pose, duplicateStateBaseNames))
                {
                    var commit = FindState(layer, "CommitExclusive_" + variant.StateName);
                    if (expected.Count == 0)
                    {
                        if (commit != null)
                        {
                            report.Error(
                                PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                                "同じ共有バンク内の切替に不要なexclusive commitがあります: " + commit.name,
                                pose.Source);
                        }

                        continue;
                    }

                    var drivers = (commit?.behaviours ?? Array.Empty<StateMachineBehaviour>())
                        .OfType<VRCAvatarParameterDriver>()
                        .Where(driver => driver.debugString == "PoseTune Commit Exclusive Pose")
                        .ToList();
                    var actual = drivers
                        .SelectMany(driver => driver.parameters)
                        .Where(parameter =>
                            parameter.type == VRC_AvatarParameterDriver.ChangeType.Set &&
                            Mathf.Approximately(parameter.value, 0f))
                        .Select(parameter => parameter.name)
                        .ToHashSet(StringComparer.Ordinal);
                    if (commit == null ||
                        drivers.Count != 1 ||
                        !drivers[0].localOnly ||
                        actual.Contains(currentParameter) ||
                        !actual.SetEquals(expected))
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                            $"Shared exclusive reset がowner-onlyの物理channel集合と一致しません: {variant.StateName}",
                            pose.Source);
                    }
                }
            }
        }

        private static void ValidateAutoPreemption(
            PoseGraph graph,
            ControllerView controller,
            PoseGroupDefinition group,
            PoseSelectionPlan poseSelection,
            ValidationReport report)
        {
            if (!graph.RootComponent.enableAutoContextSwitch ||
                group.ActivationMode == PoseGroupActivationMode.Manual)
            {
                return;
            }

            var records = new List<ExpectedAutoPoseRecord>();
            var buckets = PoseTuneLayerNaming.LayerBuckets(group);
            foreach (var pose in OrderPosesForAuto(group.Poses))
            {
                var bucket = buckets.First(candidate => candidate.Poses.Contains(pose));
                var layer = Layers(controller).LastOrDefault(candidate =>
                    candidate != null && candidate.name == bucket.LayerName);
                if (layer?.stateMachine == null)
                {
                    continue;
                }

                var duplicates = PoseStateNaming.DuplicateBaseNames(bucket.Poses);
                records.Add(new ExpectedAutoPoseRecord
                {
                    Pose = pose,
                    Layer = layer,
                    Variants = ExpectedVariants(graph, group, pose, duplicates)
                });
            }

            var modeParameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var voteParameter = PoseTuneNames.TrackingVoteParameter(group);
            var activeParameters = new HashSet<string>(PoseTuneLayerNaming.GroupActiveParameters(group));
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                var ownAutoEntries = (record.Layer.stateMachine.anyStateTransitions ??
                                      Array.Empty<TransitionView>())
                    .Where(transition =>
                        transition != null &&
                        transition.destinationState != null &&
                        record.Variants.Any(variant => variant.StateName == transition.destinationState.name) &&
                        HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 1f))
                    .ToList();
                foreach (var variant in record.Variants)
                {
                    var state = FindState(record.Layer, variant.StateName);
                    var handoff = FindState(record.Layer, variant.HandoffName);
                    if (state == null || handoff == null)
                    {
                        continue;
                    }

                    if (ownAutoEntries.Count > 0 &&
                        group.AutoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup &&
                        !(state.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                            transition != null &&
                            transition.destinationState == handoff &&
                            HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 1f) &&
                            HasCondition(
                                transition,
                                poseSelection.Find(record.Pose).ParameterName,
                                AnimatorConditionMode.NotEqual,
                                poseSelection.Find(record.Pose).Value)))
                    {
                        report.Error(
                            PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                            "SelectedPose 変更時に handoff を通る遷移がありません: " + variant.StateName,
                            record.Pose.Source);
                    }
                }

                if (index == 0 || ownAutoEntries.Count == 0)
                {
                    continue;
                }

                var higherEntries = records
                    .Take(index)
                    .SelectMany(higher =>
                        (higher.Layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>())
                        .Where(transition =>
                            transition != null &&
                            transition.destinationState != null &&
                            higher.Variants.Any(variant => variant.StateName == transition.destinationState.name) &&
                            HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 1f)))
                    .ToList();
                foreach (var variant in record.Variants)
                {
                    var state = FindState(record.Layer, variant.StateName);
                    var handoff = FindState(record.Layer, variant.HandoffName);
                    if (state == null || handoff == null)
                    {
                        continue;
                    }

                    foreach (var higherEntry in higherEntries)
                    {
                        var requiredConditions = (higherEntry.conditions ?? Array.Empty<AnimatorCondition>())
                            .Where(condition =>
                                !(condition.parameter == voteParameter &&
                                  condition.mode == AnimatorConditionMode.Equals &&
                                  Mathf.Approximately(condition.threshold, 0f)) &&
                                !(activeParameters.Contains(condition.parameter) &&
                                  condition.mode == AnimatorConditionMode.Less &&
                                  Mathf.Approximately(condition.threshold, 0.5f)))
                            .ToList();
                        if ((state.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                                transition != null &&
                                transition.destinationState == handoff &&
                                ContainsConditions(transition, requiredConditions)))
                        {
                            continue;
                        }

                        report.Error(
                            PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                            "上位 Auto Pose 成立時に handoff を通る preemption がありません: " + variant.StateName,
                            record.Pose.Source);
                    }
                }
            }
        }

        private static IEnumerable<PoseDefinition> OrderPosesForAuto(IEnumerable<PoseDefinition> poses)
        {
            return (poses ?? Enumerable.Empty<PoseDefinition>())
                .Where(pose => pose != null)
                .OrderByDescending(pose => AutoPriorityRank(pose.Priority))
                .ThenByDescending(pose => pose.Initial)
                .ThenBy(pose => pose.MenuOrder)
                .ThenBy(pose => pose.DisplayName)
                .ThenBy(pose => pose.Id);
        }

        private static int AutoPriorityRank(PoseClipPriority priority)
        {
            switch (priority)
            {
                case PoseClipPriority.High:
                    return 2;
                case PoseClipPriority.Low:
                    return 0;
                default:
                    return 1;
            }
        }

        private static bool ContainsConditions(
            TransitionView transition,
            IEnumerable<AnimatorCondition> required)
        {
            var actual = transition?.conditions ?? Array.Empty<AnimatorCondition>();
            return required.All(expected => actual.Any(condition =>
                condition.parameter == expected.parameter &&
                condition.mode == expected.mode &&
                Mathf.Approximately(condition.threshold, expected.threshold)));
        }

        private static void ValidatePoseHandoff(
            PoseGraph graph,
            PoseGroupDefinition group,
            LayerView layer,
            PoseDefinition pose,
            HashSet<string> duplicateStateBaseNames,
            ValidationReport report)
        {
            foreach (var variant in ExpectedVariants(graph, group, pose, duplicateStateBaseNames))
            {
                var state = FindState(layer, variant.StateName);
                var handoff = FindState(layer, variant.HandoffName);
                if (state == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                        "Pose variant state が見つかりません: " + variant.StateName,
                        pose.Source);
                    continue;
                }

                if (handoff == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                        "Pose handoff state が見つかりません: " + variant.HandoffName,
                        pose.Source);
                    continue;
                }

                if (!(state.transitions ?? Array.Empty<TransitionView>())
                    .Any(transition => transition != null && transition.destinationState == handoff))
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                        $"Pose variant から handoff への遷移がありません: {variant.StateName} -> {variant.HandoffName}",
                        pose.Source);
                }

                ValidateVrModeInvalidExit(pose, variant, state, handoff, report);

                var vote = TrackingPolicyUtility.IsNoChange(variant.Policy)
                    ? null
                    : graph.TrackingVotes.Votes.SingleOrDefault(candidate =>
                        candidate.GroupId == (group.Id ?? "") &&
                        TrackingPolicyUtility.AreEqual(candidate.Policy, variant.Policy));
                var voteWrites = ParametersSetBy(state)
                    .Where(parameter => parameter.name == PoseTuneNames.TrackingVoteParameter(group))
                    .ToList();
                var voteIsValid = TrackingPolicyUtility.IsNoChange(variant.Policy)
                    ? voteWrites.Count == 0
                    : vote != null && voteWrites.Any(parameter => Mathf.Approximately(parameter.value, vote.Id));
                if (!voteIsValid)
                {
                    report.Error(
                        PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                        "Pose variant が対応する tracking policy profile ID を設定していません: " + variant.StateName,
                        pose.Source);
                }

                ValidateHandoffDriver(graph, group, pose, handoff, variant.Policy, report);
            }
        }

        private static List<ExpectedPoseVariant> ExpectedVariants(
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            HashSet<string> duplicateStateBaseNames)
        {
            var needsDesktopLowerBodyLockVariant =
                PoseStateVariantRules.NeedsDesktopLowerBodyLockVariant(graph.RootComponent, group, pose);
            var needsPoseSpaceVrVariant = PoseStateVariantRules.NeedsPoseSpaceVrVariant(pose);
            var basePolicy = PoseStateVariantRules.LocksExistingDesktopPoseState(
                graph.RootComponent,
                group,
                pose)
                ? PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(group.TrackingPolicy)
                : group.TrackingPolicy;
            if (!group.EmitTrackingControl)
            {
                basePolicy = TrackingPolicyUtility.NoChange();
            }
            AnimatorConditionMode? baseVrModeInvalidExit = null;
            var baseVrModeInvalidExitThreshold = 0f;
            if (needsDesktopLowerBodyLockVariant)
            {
                baseVrModeInvalidExit = AnimatorConditionMode.Less;
                baseVrModeInvalidExitThreshold = 1f;
            }
            else if (needsPoseSpaceVrVariant)
            {
                baseVrModeInvalidExit = AnimatorConditionMode.Greater;
            }

            var variants = new List<ExpectedPoseVariant>
            {
                Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames),
                    basePolicy,
                    baseVrModeInvalidExit,
                    baseVrModeInvalidExitThreshold)
            };
            if (needsDesktopLowerBodyLockVariant)
            {
                variants.Add(Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_Desktop"),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_Desktop"),
                    PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(group.TrackingPolicy),
                    AnimatorConditionMode.Greater,
                    0f));
            }

            if (needsPoseSpaceVrVariant)
            {
                variants.Add(Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_VR"),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_VR"),
                    group.EmitTrackingControl
                        ? group.TrackingPolicy
                        : TrackingPolicyUtility.NoChange(),
                    AnimatorConditionMode.Less,
                    1f));
            }

            if (group.HasFullBodyTrackingOverride &&
                graph.RootComponent.advancedSettings?.allowFullBodyTracking == true)
            {
                variants.Add(Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, "_FBT"),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, "_FBT"),
                    group.EmitTrackingControl
                        ? group.FullBodyTrackingPolicy
                        : TrackingPolicyUtility.NoChange()));
            }

            return variants;
        }

        private static ExpectedPoseVariant Variant(
            string stateName,
            string handoffName,
            TrackingPolicyData policy,
            AnimatorConditionMode? vrModeInvalidExit = null,
            float vrModeInvalidExitThreshold = 0f)
        {
            return new ExpectedPoseVariant
            {
                StateName = stateName,
                HandoffName = handoffName,
                VrModeInvalidExit = vrModeInvalidExit,
                VrModeInvalidExitThreshold = vrModeInvalidExitThreshold,
                Policy = TrackingPolicyUtility.Copy(policy)
            };
        }

        private static void ValidateVrModeInvalidExit(
            PoseDefinition pose,
            ExpectedPoseVariant variant,
            StateView state,
            StateView handoff,
            ValidationReport report)
        {
            if (!variant.VrModeInvalidExit.HasValue)
            {
                return;
            }

            var vrModeInvalidExits = (state.transitions ?? Array.Empty<TransitionView>())
                .Where(transition => transition != null && transition.destinationState == handoff)
                .Select(transition => transition.conditions ?? Array.Empty<AnimatorCondition>())
                .Where(conditions => conditions.Length == 1 && conditions[0].parameter == "VRMode")
                .ToList();
            if (vrModeInvalidExits.Count == 1 &&
                vrModeInvalidExits[0][0].mode == variant.VrModeInvalidExit.Value &&
                Mathf.Approximately(vrModeInvalidExits[0][0].threshold, variant.VrModeInvalidExitThreshold))
            {
                return;
            }

            report.Error(
                PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                $"Pose variant の VRMode invalid exit が不足または条件不正です: {variant.StateName} -> {variant.HandoffName}",
                pose.Source);
        }

        private static void ValidateHandoffDriver(
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            StateView handoff,
            TrackingPolicyData outgoingPolicy,
            ValidationReport report)
        {
            var writes = ParametersSetBy(handoff).ToList();
            var voteName = PoseTuneNames.TrackingVoteParameter(group);
            if (group.EmitTrackingControl && ParameterAllocator.RequiresTrackingVote(graph, group) &&
                !writes.Any(parameter => parameter.name == voteName && Mathf.Approximately(parameter.value, 0f)))
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    "Pose handoff が tracking vote を clear していません: " + handoff.name,
                    pose.Source);
            }

            var expectedResets = group.GenerateResetOnExit
                ? new HashSet<string>(TrackingArbiterCompiler.ControlledParts(outgoingPolicy)
                    .Select(PoseTuneNames.TrackingResetParameter))
                : new HashSet<string>();
            var actualResets = new HashSet<string>(writes
                .Where(parameter => parameter.name != null &&
                                    parameter.name.StartsWith("PTI/TrackingReset/", StringComparison.Ordinal) &&
                                    parameter.value > 0.5f)
                .Select(parameter => parameter.name));
            if (!expectedResets.SetEquals(actualResets))
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Pose handoff の reset request が不正です: {handoff.name} " +
                    $"(actual={string.Join(",", actualResets)}, expected={string.Join(",", expectedResets)})",
                    pose.Source);
            }
        }

        private static void ValidateFbtGuard(
            PoseGraph graph,
            LayerView layer,
            PoseDefinition pose,
            HashSet<string> duplicateStateBaseNames,
            ValidationReport report)
        {
            var poseState = FindState(layer, PoseStateNaming.Name(pose, duplicateStateBaseNames));
            if (poseState == null ||
                !graph.RootComponent.disableWhenFullBodyTracking ||
                graph.RootComponent.advancedSettings?.allowFullBodyTracking == true)
            {
                return;
            }

            if ((layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>())
                .Where(transition => transition != null && transition.destinationState == poseState)
                .Any(transition => !HasFbtGuard(transition)))
            {
                report.Warning(
                    PoseTuneDiagnostics.AnimatorFbtPoseEntryRisk.Code,
                    "FBT ユーザーがこのポーズに入る可能性があります。",
                    pose.Source);
            }
        }

        private static IEnumerable<ParameterWriteView> ParametersSetBy(StateView state)
        {
            return state?.parameterWrites ?? Array.Empty<ParameterWriteView>();
        }

        private static bool HasCondition(
            TransitionView transition,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            return (transition?.conditions ?? Array.Empty<AnimatorCondition>()).Any(condition =>
                condition.parameter == parameter &&
                condition.mode == mode &&
                Mathf.Approximately(condition.threshold, threshold));
        }

        private static StateView FindState(LayerView layer, string stateName)
        {
            return (layer?.stateMachine?.states ?? Array.Empty<StateView>())
                .FirstOrDefault(state => state != null && state.name == stateName);
        }

        private static bool HasFbtGuard(TransitionView transition)
        {
            var conditions = transition?.conditions ?? Array.Empty<AnimatorCondition>();
            var hasUpperGuard = conditions.Any(condition =>
                condition.parameter == "TrackingType" &&
                condition.mode == AnimatorConditionMode.Less &&
                condition.threshold < 4.0001f);
            var hasLowerGuard = conditions.Any(condition =>
                condition.parameter == "TrackingType" &&
                condition.mode == AnimatorConditionMode.Greater &&
                condition.threshold > 1.9999f);
            return hasUpperGuard && hasLowerGuard;
        }

        private static IEnumerable<LayerView> Layers(ControllerView controller)
        {
            return controller?.layers ?? Array.Empty<LayerView>();
        }

        private static string NormalizeInternalParameterName(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                !name.StartsWith("PTI/", StringComparison.Ordinal))
            {
                return name;
            }

            var localizationSuffix = name.IndexOf('$');
            return localizationSuffix < 0 ? name : name.Substring(0, localizationSuffix);
        }

        private static AnimatorControllerParameter ParameterView(
            string name,
            AnimatorControllerParameter parameter)
        {
            return new AnimatorControllerParameter
            {
                name = NormalizeInternalParameterName(name),
                type = parameter.type,
                defaultBool = parameter.defaultBool,
                defaultFloat = parameter.defaultFloat,
                defaultInt = parameter.defaultInt
            };
        }

        private static AnimatorCondition[] ConditionViews(IEnumerable<AnimatorCondition> conditions)
        {
            return (conditions ?? Enumerable.Empty<AnimatorCondition>())
                .Select(condition => new AnimatorCondition
                {
                    parameter = NormalizeInternalParameterName(condition.parameter),
                    mode = condition.mode,
                    threshold = condition.threshold
                })
                .ToArray();
        }

        // NDMF keeps playable layers virtualized while Transforming passes are running.  The
        // validator intentionally reads through this small immutable view so the generated
        // controller and the post-MA virtual controller are checked by exactly the same rules.
        private sealed class ControllerView
        {
            public AnimatorControllerParameter[] parameters = Array.Empty<AnimatorControllerParameter>();
            public LayerView[] layers = Array.Empty<LayerView>();

            public static ControllerView Create(AnimatorController controller)
            {
                if (controller == null)
                {
                    return null;
                }

                return new ControllerView
                {
                    parameters = (controller.parameters ?? Array.Empty<AnimatorControllerParameter>())
                        .Where(parameter => parameter != null)
                        .Select(parameter => ParameterView(parameter.name, parameter))
                        .ToArray(),
                    layers = (controller.layers ?? Array.Empty<AnimatorControllerLayer>())
                        .Where(layer => layer != null)
                        .Select(LayerView.Create)
                        .ToArray()
                };
            }

            public static ControllerView Create(VirtualAnimatorController controller)
            {
                if (controller == null)
                {
                    return null;
                }

                return new ControllerView
                {
                    parameters = controller.Parameters
                        .Select(parameter => ParameterView(parameter.Key, parameter.Value))
                        .ToArray(),
                    layers = controller.Layers
                        .Where(layer => layer != null)
                        .Select(LayerView.Create)
                        .ToArray()
                };
            }
        }

        private sealed class LayerView
        {
            public string name;
            public StateMachineView stateMachine;

            public static LayerView Create(AnimatorControllerLayer layer)
            {
                return new LayerView
                {
                    name = layer.name,
                    stateMachine = StateMachineView.Create(layer.stateMachine)
                };
            }

            public static LayerView Create(VirtualLayer layer)
            {
                return new LayerView
                {
                    name = layer.Name,
                    stateMachine = StateMachineView.Create(layer.StateMachine)
                };
            }
        }

        private sealed class StateMachineView
        {
            public StateView[] states = Array.Empty<StateView>();
            public TransitionView[] anyStateTransitions = Array.Empty<TransitionView>();

            public static StateMachineView Create(AnimatorStateMachine stateMachine)
            {
                if (stateMachine == null)
                {
                    return null;
                }

                var sourceStates = (stateMachine.states ?? Array.Empty<ChildAnimatorState>())
                    .Select(child => child.state)
                    .Where(state => state != null)
                    .ToList();
                var views = sourceStates.ToDictionary(state => state, StateView.Create);
                StateView Resolve(AnimatorState state)
                {
                    if (state == null)
                    {
                        return null;
                    }

                    if (!views.TryGetValue(state, out var view))
                    {
                        view = StateView.Create(state);
                        views.Add(state, view);
                    }

                    return view;
                }

                foreach (var state in sourceStates)
                {
                    views[state].transitions = (state.transitions ?? Array.Empty<AnimatorStateTransition>())
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray();
                }

                return new StateMachineView
                {
                    states = sourceStates.Select(Resolve).ToArray(),
                    anyStateTransitions = (stateMachine.anyStateTransitions ??
                                           Array.Empty<AnimatorStateTransition>())
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray()
                };
            }

            public static StateMachineView Create(VirtualStateMachine stateMachine)
            {
                if (stateMachine == null)
                {
                    return null;
                }

                var sourceStates = stateMachine.States
                    .Select(child => child.State)
                    .Where(state => state != null)
                    .ToList();
                var views = sourceStates.ToDictionary(state => state, StateView.Create);
                StateView Resolve(VirtualState state)
                {
                    if (state == null)
                    {
                        return null;
                    }

                    if (!views.TryGetValue(state, out var view))
                    {
                        view = StateView.Create(state);
                        views.Add(state, view);
                    }

                    return view;
                }

                foreach (var state in sourceStates)
                {
                    views[state].transitions = state.Transitions
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray();
                }

                return new StateMachineView
                {
                    states = sourceStates.Select(Resolve).ToArray(),
                    anyStateTransitions = stateMachine.AnyStateTransitions
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray()
                };
            }
        }

        private sealed class StateView
        {
            public string name;
            public StateMachineBehaviour[] behaviours = Array.Empty<StateMachineBehaviour>();
            public ParameterWriteView[] parameterWrites = Array.Empty<ParameterWriteView>();
            public TransitionView[] transitions = Array.Empty<TransitionView>();

            public static StateView Create(AnimatorState state)
            {
                return new StateView
                {
                    name = state.name,
                    behaviours = state.behaviours ?? Array.Empty<StateMachineBehaviour>(),
                    parameterWrites = ParameterWriteView.Create(
                        state.behaviours ?? Array.Empty<StateMachineBehaviour>())
                };
            }

            public static StateView Create(VirtualState state)
            {
                return new StateView
                {
                    name = state.Name,
                    behaviours = state.Behaviours.ToArray(),
                    parameterWrites = ParameterWriteView.Create(state.Behaviours)
                };
            }
        }

        private sealed class ParameterWriteView
        {
            public string name;
            public float value;

            public static ParameterWriteView[] Create(IEnumerable<StateMachineBehaviour> behaviours)
            {
                return (behaviours ?? Enumerable.Empty<StateMachineBehaviour>())
                    .OfType<VRCAvatarParameterDriver>()
                    .SelectMany(driver =>
                        driver.parameters ?? new List<VRC_AvatarParameterDriver.Parameter>())
                    .Where(parameter => parameter != null &&
                                        parameter.type == VRC_AvatarParameterDriver.ChangeType.Set)
                    .Select(parameter => new ParameterWriteView
                    {
                        name = NormalizeInternalParameterName(parameter.name),
                        value = parameter.value
                    })
                    .ToArray();
            }
        }

        private sealed class TransitionView
        {
            public StateView destinationState;
            public AnimatorCondition[] conditions = Array.Empty<AnimatorCondition>();
            public bool canTransitionToSelf;

            public static TransitionView Create(
                AnimatorStateTransition transition,
                Func<AnimatorState, StateView> resolve)
            {
                return new TransitionView
                {
                    destinationState = resolve(transition.destinationState),
                    conditions = ConditionViews(transition.conditions),
                    canTransitionToSelf = transition.canTransitionToSelf
                };
            }

            public static TransitionView Create(
                VirtualStateTransition transition,
                Func<VirtualState, StateView> resolve)
            {
                return new TransitionView
                {
                    destinationState = resolve(transition.DestinationState),
                    conditions = ConditionViews(transition.Conditions),
                    canTransitionToSelf = transition.CanTransitionToSelf
                };
            }
        }

        private sealed class ExpectedPoseVariant
        {
            public string StateName;
            public string HandoffName;
            public AnimatorConditionMode? VrModeInvalidExit;
            public float VrModeInvalidExitThreshold;
            public TrackingPolicyData Policy;
        }

        private sealed class ExpectedArbiterVote
        {
            public string Parameter;
            public int VoteId;
            public TrackingMode Mode;
        }

        private sealed class ExpectedAutoPoseRecord
        {
            public PoseDefinition Pose;
            public LayerView Layer;
            public List<ExpectedPoseVariant> Variants;
        }
    }
}
