using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class TrackingArbiterCompiler
    {
        private static readonly TrackingPart[] Parts = (TrackingPart[])Enum.GetValues(typeof(TrackingPart));

        public static IReadOnlyList<TrackingPart> RequiredParts(PoseGraph graph)
        {
            var required = new HashSet<TrackingPart>();
            if (graph == null)
            {
                return Array.Empty<TrackingPart>();
            }

            foreach (var pose in graph.Poses.Where(pose => pose?.Group?.EmitTrackingControl == true))
            {
                required.UnionWith(ControlledParts(graph, pose.Group, pose));
            }

            if (graph.HasPoseOptions)
            {
                required.Add(TrackingPart.Head);
                required.Add(TrackingPart.LeftHand);
                required.Add(TrackingPart.RightHand);
                required.Add(TrackingPart.LeftFoot);
                required.Add(TrackingPart.RightFoot);
                required.Add(TrackingPart.LeftFingers);
                required.Add(TrackingPart.RightFingers);
            }

            return Parts.Where(required.Contains).ToList();
        }

        public static IReadOnlyList<TrackingPart> ControlledParts(
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose)
        {
            var controlled = new HashSet<TrackingPart>();
            if (graph == null || pose == null || group == null || !group.EmitTrackingControl)
            {
                return Array.Empty<TrackingPart>();
            }

            AddControlledParts(controlled, group.TrackingPolicy);
            if (PoseStateVariantRules.NeedsDesktopLowerBodyLockVariant(graph.RootComponent, group, pose) ||
                PoseStateVariantRules.LocksExistingDesktopPoseState(graph.RootComponent, group, pose))
            {
                controlled.Add(TrackingPart.Hip);
                controlled.Add(TrackingPart.LeftFoot);
                controlled.Add(TrackingPart.RightFoot);
            }

            if (group.HasFullBodyTrackingOverride &&
                graph.RootComponent?.advancedSettings?.allowFullBodyTracking == true)
            {
                AddControlledParts(controlled, group.FullBodyTrackingPolicy);
            }

            return Parts.Where(controlled.Contains).ToList();
        }

        public static IReadOnlyList<TrackingPart> ControlledParts(TrackingPolicyData policy)
        {
            var controlled = new HashSet<TrackingPart>();
            AddControlledParts(controlled, policy);
            return Parts.Where(controlled.Contains).ToList();
        }

        public static void Compile(AnimatorBuildResult result, PoseGraph graph)
        {
            foreach (var part in RequiredParts(graph))
            {
                CompilePart(result, graph, part);
            }
        }

        private static void CompilePart(AnimatorBuildResult result, PoseGraph graph, TrackingPart part)
        {
            var layer = AnimatorLayerFactory.NewLayer(PoseTuneNames.TrackingArbiterLayerName(part));
            var empty = AnimatorLayerFactory.EmptyClip(layer.name + "_Empty");
            var hold = AnimatorLayerFactory.ResetHoldClip(layer.name + "_Hold", 0.02f);
            result.GeneratedAssets.Add(empty);
            result.GeneratedAssets.Add(hold);

            var idle = AddState(layer, "Idle", new Vector3(240, 80), empty, part, TrackingMode.NoChange);
            var animation = AddState(layer, "VoteAnimation", new Vector3(500, 80), empty, part,
                TrackingMode.Animation);
            var tracking = AddState(layer, "VoteTracking", new Vector3(500, 180), empty, part,
                TrackingMode.Tracking);
            var locked = AddState(layer, "LockedAnimation", new Vector3(500, -20), empty, part,
                TrackingMode.Animation);
            var requestAnimation = AddRequestState(layer, "RequestAnimation", new Vector3(760, -20), hold,
                idle, part, TrackingMode.Animation);
            var requestTracking = AddRequestState(layer, "RequestTracking", new Vector3(760, 100), hold,
                idle, part, TrackingMode.Tracking);
            var requestReset = AddRequestState(layer, "RequestReset", new Vector3(760, 220), hold,
                idle, part, TrackingMode.Tracking);
            var lockRelease = AddState(layer, "LockRelease", new Vector3(760, 340), hold, part,
                TrackingMode.Tracking);
            AddReturnToIdle(lockRelease, idle);
            layer.stateMachine.defaultState = idle;

            var candidates = VoteCandidates(graph, part);
            var activePoseParameters = graph.HasPoseOptions
                ? PoseTuneLayerNaming.GroupActiveParameters(graph).ToList()
                : new List<string>();
            var animationCandidates = candidates.Where(candidate => candidate.Mode == TrackingMode.Animation).ToList();
            var trackingCandidates = candidates.Where(candidate => candidate.Mode == TrackingMode.Tracking).ToList();
            var lockParameter = LockParameter(graph, part);
            var resetParameter = PoseTuneNames.TrackingResetParameter(part);

            if (!string.IsNullOrWhiteSpace(lockParameter))
            {
                foreach (var activePoseParameter in activePoseParameters)
                {
                    var transition = AddAnyTransition(layer, requestAnimation);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, resetParameter);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, lockParameter);
                    AddPoseActiveCondition(transition, activePoseParameter);
                }
            }

            foreach (var candidate in animationCandidates)
            {
                var transition = AddAnyTransition(layer, requestAnimation);
                transition.AddCondition(AnimatorConditionMode.If, 0f, resetParameter);
                AddLockDisabledCondition(transition, lockParameter);
                AddVoteCondition(transition, candidate);
            }

            foreach (var candidate in trackingCandidates)
            {
                var transition = AddAnyTransition(layer, requestTracking);
                transition.AddCondition(AnimatorConditionMode.If, 0f, resetParameter);
                AddLockDisabledCondition(transition, lockParameter);
                AddNoModeConditions(transition, animationCandidates);
                AddVoteCondition(transition, candidate);
            }

            var resetWithoutWinner = AddAnyTransition(layer, requestReset);
            resetWithoutWinner.AddCondition(AnimatorConditionMode.If, 0f, resetParameter);
            AddNoModeConditions(resetWithoutWinner, animationCandidates);
            AddNoModeConditions(resetWithoutWinner, trackingCandidates);

            if (!string.IsNullOrWhiteSpace(lockParameter))
            {
                foreach (var activePoseParameter in activePoseParameters)
                {
                    var transition = AddAnyTransition(layer, locked);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, lockParameter);
                    AddPoseActiveCondition(transition, activePoseParameter);
                }
            }

            foreach (var candidate in animationCandidates)
            {
                var transition = AddAnyTransition(layer, animation);
                AddLockDisabledCondition(transition, lockParameter);
                AddVoteCondition(transition, candidate);
            }

            foreach (var candidate in trackingCandidates)
            {
                var transition = AddAnyTransition(layer, tracking);
                AddLockDisabledCondition(transition, lockParameter);
                AddNoModeConditions(transition, animationCandidates);
                AddVoteCondition(transition, candidate);
            }

            AddNoWinnerReturn(animation, idle, animationCandidates, trackingCandidates);
            AddNoWinnerReturn(tracking, idle, animationCandidates, trackingCandidates);

            if (!string.IsNullOrWhiteSpace(lockParameter))
            {
                var releaseWhenLockTurnsOff = locked.AddTransition(lockRelease);
                releaseWhenLockTurnsOff.hasExitTime = false;
                releaseWhenLockTurnsOff.duration = 0f;
                releaseWhenLockTurnsOff.AddCondition(AnimatorConditionMode.IfNot, 0f, lockParameter);
                AddNoModeConditions(releaseWhenLockTurnsOff, animationCandidates);
                AddNoModeConditions(releaseWhenLockTurnsOff, trackingCandidates);

                var releaseWhenLastPoseExits = locked.AddTransition(lockRelease);
                releaseWhenLastPoseExits.hasExitTime = false;
                releaseWhenLastPoseExits.duration = 0f;
                AddNoActivePoseConditions(releaseWhenLastPoseExits, activePoseParameters);
            }

            result.TargetController.AddLayer(layer);
        }

        private static AnimatorState AddState(
            AnimatorControllerLayer layer,
            string name,
            Vector3 position,
            Motion motion,
            TrackingPart part,
            TrackingMode mode)
        {
            var state = layer.stateMachine.AddState(name, position);
            state.motion = motion;
            TrackingCompiler.AddTrackingBehavior(state, part, mode);
            return state;
        }

        private static AnimatorState AddRequestState(
            AnimatorControllerLayer layer,
            string name,
            Vector3 position,
            Motion motion,
            AnimatorState idle,
            TrackingPart part,
            TrackingMode mode)
        {
            var state = AddState(layer, name, position, motion, part, mode);
            ParameterDriverCompiler.ClearTrackingReset(state, part);
            AddReturnToIdle(state, idle);
            return state;
        }

        private static void AddReturnToIdle(AnimatorState state, AnimatorState idle)
        {
            var transition = state.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
        }

        private static void AddNoWinnerReturn(
            AnimatorState state,
            AnimatorState idle,
            IEnumerable<TrackingVoteCandidate> animationCandidates,
            IEnumerable<TrackingVoteCandidate> trackingCandidates)
        {
            var transition = state.AddTransition(idle);
            transition.hasExitTime = false;
            transition.duration = 0f;
            AddNoModeConditions(transition, animationCandidates);
            AddNoModeConditions(transition, trackingCandidates);
        }

        private static AnimatorStateTransition AddAnyTransition(
            AnimatorControllerLayer layer,
            AnimatorState destination)
        {
            var transition = layer.stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static List<TrackingVoteCandidate> VoteCandidates(PoseGraph graph, TrackingPart part)
        {
            return ActiveVoteCandidates(graph)
                .Select(candidate => new TrackingVoteCandidate
                {
                    Group = candidate.Group,
                    VoteId = candidate.VoteId,
                    Mode = TrackingCompiler.Mode(candidate.Policy, part)
                })
                .Where(candidate => candidate.Mode != TrackingMode.NoChange)
                .ToList();
        }

        private static List<ActiveTrackingVoteCandidate> ActiveVoteCandidates(PoseGraph graph)
        {
            var groupsById = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => group != null)
                .GroupBy(group => group.Id ?? "")
                .ToDictionary(group => group.Key, group => group.First());
            var candidates = new List<ActiveTrackingVoteCandidate>();
            foreach (var vote in graph.TrackingVotes.Votes)
            {
                if (!groupsById.TryGetValue(vote.GroupId ?? "", out var group))
                {
                    continue;
                }

                candidates.Add(new ActiveTrackingVoteCandidate
                {
                    Group = group,
                    VoteId = vote.Id,
                    Policy = vote.Policy
                });
            }

            return candidates;
        }

        private static void AddVoteCondition(AnimatorStateTransition transition, TrackingVoteCandidate candidate)
        {
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                candidate.VoteId,
                PoseTuneNames.TrackingVoteParameter(candidate.Group));
        }

        private static void AddVoteCondition(AnimatorStateTransition transition, ActiveTrackingVoteCandidate candidate)
        {
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                candidate.VoteId,
                PoseTuneNames.TrackingVoteParameter(candidate.Group));
        }

        private static void AddNoModeConditions(
            AnimatorStateTransition transition,
            IEnumerable<TrackingVoteCandidate> candidates)
        {
            foreach (var candidate in candidates)
            {
                transition.AddCondition(
                    AnimatorConditionMode.NotEqual,
                    candidate.VoteId,
                    PoseTuneNames.TrackingVoteParameter(candidate.Group));
            }
        }

        private static void AddPoseActiveCondition(
            AnimatorStateTransition transition,
            string activePoseParameter)
        {
            transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, activePoseParameter);
        }

        private static void AddNoActivePoseConditions(
            AnimatorStateTransition transition,
            IEnumerable<string> activePoseParameters)
        {
            foreach (var activePoseParameter in activePoseParameters)
            {
                transition.AddCondition(AnimatorConditionMode.Less, 0.5f, activePoseParameter);
            }
        }

        private static void AddLockDisabledCondition(AnimatorStateTransition transition, string lockParameter)
        {
            if (!string.IsNullOrWhiteSpace(lockParameter))
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, lockParameter);
            }
        }

        private static string LockParameter(PoseGraph graph, TrackingPart part)
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

        private static void AddControlledParts(ISet<TrackingPart> parts, TrackingPolicyData policy)
        {
            foreach (var part in Parts)
            {
                if (TrackingCompiler.Mode(policy, part) != TrackingMode.NoChange)
                {
                    parts.Add(part);
                }
            }
        }

        private sealed class TrackingVoteCandidate
        {
            public PoseGroupDefinition Group;
            public int VoteId;
            public TrackingMode Mode;
        }

        private sealed class ActiveTrackingVoteCandidate
        {
            public PoseGroupDefinition Group;
            public int VoteId;
            public TrackingPolicyData Policy;
        }
    }
}
