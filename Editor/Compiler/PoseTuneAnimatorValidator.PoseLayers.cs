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
        private static void ValidateGroupLayer(
            PoseGraph graph,
            ControllerView controller,
            PoseGroupDefinition group,
            string layerName,
            List<PoseDefinition> poses,
            PoseSelectionPlan poseSelection,
            ValidationReport report)
        {
            var layer = FindLayer(controller, layerName);
            if (layer?.stateMachine == null)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorMissingResetExitTransition.Code,
                    "Pose layer が見つかりません: " + layerName,
                    graph.RootComponent);
                return;
            }

            var duplicateStateBaseNames = PoseStateNaming.DuplicateBaseNames(poses);
            // ExpectedVariants is pure: compute it once per pose for every check that uses this bucket's duplicate
            // names. A list (not a dictionary) keeps pose order and tolerates a repeated pose reference.
            var poseVariants = poses
                .Select(pose => (Pose: pose, Variants: ExpectedVariants(graph, group, pose, duplicateStateBaseNames)))
                .ToList();
            foreach (var (pose, variants) in poseVariants)
            {
                ValidatePoseHandoff(graph, group, layer, pose, variants, report);
                ValidateFbtGuard(graph, layer, pose, duplicateStateBaseNames, report);
                ValidatePoseSelectionTransitions(
                    graph,
                    group,
                    layer,
                    pose,
                    variants,
                    poseSelection,
                    report);
            }

            ValidateSharedExclusiveResetDrivers(
                graph,
                group,
                layer,
                poseVariants,
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

            var activeParameters = PoseTuneLayerNaming.GroupActiveParameters(group).ToList();
            foreach (var transition in layer.stateMachine.anyStateTransitions ??
                         Array.Empty<TransitionView>())
            {
                foreach (var activeParameter in activeParameters)
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
            IEnumerable<ExpectedPoseVariant> variants,
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
            foreach (var variant in variants)
            {
                var hasEntry = anyTransitions.Any(transition =>
                    transition?.destinationState != null &&
                    (transition.destinationState.name == variant.StateName ||
                     transition.destinationState.name == PoseStateNaming.CommitExclusivePrefix + variant.StateName) &&
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
            IEnumerable<(PoseDefinition Pose, List<ExpectedPoseVariant> Variants)> poseVariants,
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
            foreach (var (pose, variants) in poseVariants)
            {
                foreach (var variant in variants)
                {
                    var commit = FindState(layer, PoseStateNaming.CommitExclusivePrefix + variant.StateName);
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
                        .Where(driver => driver.debugString == PoseTuneNames.CommitExclusivePoseDebugString)
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
            var duplicatesByBucket = buckets.ToDictionary(bucket => bucket,
                bucket => PoseStateNaming.DuplicateBaseNames(bucket.Poses));
            foreach (var pose in OrderPosesForAuto(group.Poses))
            {
                var bucket = buckets.First(candidate => candidate.Poses.Contains(pose));
                var layer = Layers(controller).LastOrDefault(candidate =>
                    candidate != null && candidate.name == bucket.LayerName);
                if (layer?.stateMachine == null)
                {
                    continue;
                }

                var duplicates = duplicatesByBucket[bucket];
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
            // Keep one entry per original transition, including duplicates: diagnostics are per entry.
            var autoEntries = records.Select(record =>
                (record.Layer.stateMachine.anyStateTransitions ?? Array.Empty<TransitionView>())
                .Where(transition => transition != null && transition.destinationState != null &&
                    record.Variants.Any(variant => variant.StateName == transition.destinationState.name) &&
                    HasCondition(transition, modeParameter, AnimatorConditionMode.Equals, 1f))
                .Select(transition => (transition.conditions ?? Array.Empty<AnimatorCondition>())
                    .Where(condition =>
                        !(condition.parameter == voteParameter &&
                          condition.mode == AnimatorConditionMode.Equals &&
                          Mathf.Approximately(condition.threshold, 0f)) &&
                        !(activeParameters.Contains(condition.parameter) &&
                          condition.mode == AnimatorConditionMode.Less &&
                          Mathf.Approximately(condition.threshold, 0.5f)))
                    .ToArray())
                .ToList()).ToList();
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                var ownAutoEntries = autoEntries[index];
                // Dereferenced only inside the predicate below, so a missing binding fails exactly where it did.
                var selectionBinding = poseSelection.Find(record.Pose);
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
                                selectionBinding.ParameterName,
                                AnimatorConditionMode.NotEqual,
                                selectionBinding.Value)))
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

                var higherEntries = autoEntries
                    .Take(index)
                    .SelectMany(entries => entries)
                    .ToList();
                foreach (var variant in record.Variants)
                {
                    var state = FindState(record.Layer, variant.StateName);
                    var handoff = FindState(record.Layer, variant.HandoffName);
                    if (state == null || handoff == null)
                    {
                        continue;
                    }

                    foreach (var requiredConditions in higherEntries)
                    {
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
            IEnumerable<ExpectedPoseVariant> variants,
            ValidationReport report)
        {
            foreach (var variant in variants)
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
            var needsDesktopVariant =
                PoseStateVariantRules.NeedsDesktopVariant(graph.RootComponent, group, pose);
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
            if (needsDesktopVariant)
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
            if (needsDesktopVariant)
            {
                variants.Add(Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, PoseStateNaming.DesktopSuffix),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, PoseStateNaming.DesktopSuffix),
                    group.EmitTrackingControl
                        ? PoseStateVariantRules.DesktopVariantTrackingPolicy(graph.RootComponent, group, pose)
                        : TrackingPolicyUtility.NoChange(),
                    AnimatorConditionMode.Greater,
                    0f));
            }

            if (needsPoseSpaceVrVariant)
            {
                variants.Add(Variant(
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, PoseStateNaming.VrSuffix),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, PoseStateNaming.VrSuffix),
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
                    PoseStateNaming.Name(pose, duplicateStateBaseNames, PoseStateNaming.FullBodySuffix),
                    PoseStateNaming.CleanupName(pose, duplicateStateBaseNames, PoseStateNaming.FullBodySuffix),
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
                                    parameter.name.StartsWith(PoseTuneNames.TrackingResetParameterPrefix, StringComparison.Ordinal) &&
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
    }
}
