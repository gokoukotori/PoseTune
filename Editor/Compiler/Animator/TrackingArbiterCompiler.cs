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

            if (PoseStateVariantRules.NeedsFullBodyVariant(graph.RootComponent, group))
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

        public static void Compile(AnimatorBuildResult result, PoseGraph graph, AnimationClip refreshLoop)
        {
            foreach (var part in RequiredParts(graph))
            {
                CompilePart(result, graph, part, refreshLoop);
            }
        }

        private static void CompilePart(
            AnimatorBuildResult result,
            PoseGraph graph,
            TrackingPart part,
            AnimationClip refreshLoop)
        {
            var layer = AnimatorLayerFactory.NewLayer(PoseTuneNames.TrackingArbiterLayerName(part));
            var states = CreateStates(result, layer, part);
            var inputs = CollectInputs(graph, part);

            // Any-state transitions are checked in creation order: reset requests outrank steady votes.
            AddResetRequestTransitions(layer, states, inputs);
            AddSteadyVoteTransitions(layer, states, inputs);
            AddNoWinnerReturn(states.Animation, states.Idle, inputs);
            AddNoWinnerReturn(states.Tracking, states.Idle, inputs);
            if (inputs.HasLock)
            {
                AddLockRelease(states, inputs);
            }

            // Added last: winner changes and lock releases above keep priority over the refresh loop.
            PoseRefreshLayerCompiler.AddRefreshLoop(states.Animation, refreshLoop);
            PoseRefreshLayerCompiler.AddRefreshLoop(states.Tracking, refreshLoop);
            PoseRefreshLayerCompiler.AddRefreshLoop(states.Locked, refreshLoop);

            result.TargetController.AddLayer(layer);
        }

        private static ArbiterLayerStates CreateStates(
            AnimatorBuildResult result,
            AnimatorControllerLayer layer,
            TrackingPart part)
        {
            var empty = AnimatorLayerFactory.EmptyClip(layer.name + "_Empty");
            var hold = AnimatorLayerFactory.ResetHoldClip(
                layer.name + "_Hold",
                AnimatorCompiler.CriticalStateHoldSeconds);
            result.GeneratedAssets.Add(empty);
            result.GeneratedAssets.Add(hold);

            var idle = AddState(layer, PoseTuneNames.ArbiterStates.Idle, new Vector3(240, 80), empty, part,
                TrackingMode.NoChange);
            var animation = AddState(layer, PoseTuneNames.ArbiterStates.VoteAnimation, new Vector3(500, 80), empty,
                part, TrackingMode.Animation);
            var tracking = AddState(layer, PoseTuneNames.ArbiterStates.VoteTracking, new Vector3(500, 180), empty,
                part, TrackingMode.Tracking);
            var locked = AddState(layer, PoseTuneNames.ArbiterStates.LockedAnimation, new Vector3(500, -20), empty,
                part, TrackingMode.Animation);
            var requestAnimation = AddRequestState(layer, PoseTuneNames.ArbiterStates.RequestAnimation,
                new Vector3(760, -20), hold, idle, part, TrackingMode.Animation);
            var requestTracking = AddRequestState(layer, PoseTuneNames.ArbiterStates.RequestTracking,
                new Vector3(760, 100), hold, idle, part, TrackingMode.Tracking);
            var requestReset = AddRequestState(layer, PoseTuneNames.ArbiterStates.RequestReset,
                new Vector3(760, 220), hold, idle, part, TrackingMode.Tracking);
            var lockRelease = AddState(layer, PoseTuneNames.ArbiterStates.LockRelease, new Vector3(760, 340), hold,
                part, TrackingMode.Tracking);
            AnimatorTransitionFactory.AfterExitTime(lockRelease, idle);
            layer.stateMachine.defaultState = idle;

            return new ArbiterLayerStates
            {
                Idle = idle,
                Animation = animation,
                Tracking = tracking,
                Locked = locked,
                RequestAnimation = requestAnimation,
                RequestTracking = requestTracking,
                RequestReset = requestReset,
                LockRelease = lockRelease
            };
        }

        private static ArbiterInputs CollectInputs(PoseGraph graph, TrackingPart part)
        {
            var candidates = VoteCandidates(graph, part);
            var activePoseParameters = graph.HasPoseOptions
                ? PoseTuneLayerNaming.GroupActiveParameters(graph).ToList()
                : new List<string>();
            var animationCandidates = candidates.Where(candidate => candidate.Mode == TrackingMode.Animation).ToList();
            var trackingCandidates = candidates.Where(candidate => candidate.Mode == TrackingMode.Tracking).ToList();
            var lockParameter = PoseTuneNames.TrackingArbiterLockParameter(graph, part);
            var resetParameter = PoseTuneNames.TrackingResetParameter(part);
            return new ArbiterInputs
            {
                ActivePoseParameters = activePoseParameters,
                AnimationCandidates = animationCandidates,
                TrackingCandidates = trackingCandidates,
                LockParameter = lockParameter,
                HasLock = !string.IsNullOrWhiteSpace(lockParameter),
                ResetParameter = resetParameter
            };
        }

        /// <summary>
        /// Routes a pending tracking reset to the request state of the current winner (an active pose under the
        /// lock, then animation votes, then tracking votes), or to RequestReset when no vote is set.
        /// </summary>
        private static void AddResetRequestTransitions(
            AnimatorControllerLayer layer,
            ArbiterLayerStates states,
            ArbiterInputs inputs)
        {
            if (inputs.HasLock)
            {
                foreach (var activePoseParameter in inputs.ActivePoseParameters)
                {
                    var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.RequestAnimation);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, inputs.ResetParameter);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, inputs.LockParameter);
                    ActiveParameterConditions.AddActiveCondition(transition, activePoseParameter);
                }
            }

            foreach (var candidate in inputs.AnimationCandidates)
            {
                var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.RequestAnimation);
                transition.AddCondition(AnimatorConditionMode.If, 0f, inputs.ResetParameter);
                AddLockDisabledCondition(transition, inputs);
                AddVoteCondition(transition, candidate);
            }

            foreach (var candidate in inputs.TrackingCandidates)
            {
                var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.RequestTracking);
                transition.AddCondition(AnimatorConditionMode.If, 0f, inputs.ResetParameter);
                AddLockDisabledCondition(transition, inputs);
                AddNoModeConditions(transition, inputs.AnimationCandidates);
                AddVoteCondition(transition, candidate);
            }

            var resetWithoutWinner = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.RequestReset);
            resetWithoutWinner.AddCondition(AnimatorConditionMode.If, 0f, inputs.ResetParameter);
            AddNoModeConditions(resetWithoutWinner, inputs.AnimationCandidates);
            AddNoModeConditions(resetWithoutWinner, inputs.TrackingCandidates);
        }

        /// <summary>
        /// Steady winner selection: an active pose under the lock, then animation votes, then tracking votes.
        /// </summary>
        private static void AddSteadyVoteTransitions(
            AnimatorControllerLayer layer,
            ArbiterLayerStates states,
            ArbiterInputs inputs)
        {
            if (inputs.HasLock)
            {
                foreach (var activePoseParameter in inputs.ActivePoseParameters)
                {
                    var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.Locked);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, inputs.LockParameter);
                    ActiveParameterConditions.AddActiveCondition(transition, activePoseParameter);
                }
            }

            foreach (var candidate in inputs.AnimationCandidates)
            {
                var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.Animation);
                AddLockDisabledCondition(transition, inputs);
                AddVoteCondition(transition, candidate);
            }

            foreach (var candidate in inputs.TrackingCandidates)
            {
                var transition = AnimatorTransitionFactory.ImmediateFromAny(layer.stateMachine, states.Tracking);
                AddLockDisabledCondition(transition, inputs);
                AddNoModeConditions(transition, inputs.AnimationCandidates);
                AddVoteCondition(transition, candidate);
            }
        }

        private static void AddLockRelease(ArbiterLayerStates states, ArbiterInputs inputs)
        {
            var releaseWhenLockTurnsOff = AnimatorTransitionFactory.Immediate(states.Locked, states.LockRelease);
            releaseWhenLockTurnsOff.AddCondition(AnimatorConditionMode.IfNot, 0f, inputs.LockParameter);
            AddNoModeConditions(releaseWhenLockTurnsOff, inputs.AnimationCandidates);
            AddNoModeConditions(releaseWhenLockTurnsOff, inputs.TrackingCandidates);

            var releaseWhenLastPoseExits = AnimatorTransitionFactory.Immediate(states.Locked, states.LockRelease);
            AddNoActivePoseConditions(releaseWhenLastPoseExits, inputs.ActivePoseParameters);
        }

        private static AnimatorState AddState(
            AnimatorControllerLayer layer,
            string name,
            Vector3 position,
            Motion motion,
            TrackingPart part,
            TrackingMode mode)
        {
            var state = AnimatorLayerFactory.AddState(layer.stateMachine, name, position, motion);
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
            AnimatorTransitionFactory.AfterExitTime(state, idle);
            return state;
        }

        private static void AddNoWinnerReturn(AnimatorState state, AnimatorState idle, ArbiterInputs inputs)
        {
            var transition = AnimatorTransitionFactory.Immediate(state, idle);
            AddNoModeConditions(transition, inputs.AnimationCandidates);
            AddNoModeConditions(transition, inputs.TrackingCandidates);
        }

        private static List<TrackingVoteCandidate> VoteCandidates(PoseGraph graph, TrackingPart part)
        {
            var groupsById = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => group != null)
                .GroupBy(group => group.Id ?? "")
                .ToDictionary(group => group.Key, group => group.First());
            var candidates = new List<TrackingVoteCandidate>();
            foreach (var vote in graph.TrackingVotes.Votes)
            {
                if (!groupsById.TryGetValue(vote.GroupId ?? "", out var group))
                {
                    continue;
                }

                var mode = TrackingPolicyUtility.Get(vote.Policy, part);
                if (mode == TrackingMode.NoChange)
                {
                    continue;
                }

                candidates.Add(new TrackingVoteCandidate
                {
                    Group = group,
                    VoteId = vote.Id,
                    Mode = mode
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

        private static void AddNoActivePoseConditions(
            AnimatorStateTransition transition,
            IEnumerable<string> activePoseParameters)
        {
            foreach (var activePoseParameter in activePoseParameters)
            {
                ActiveParameterConditions.AddInactiveCondition(transition, activePoseParameter);
            }
        }

        private static void AddLockDisabledCondition(AnimatorStateTransition transition, ArbiterInputs inputs)
        {
            if (inputs.HasLock)
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, inputs.LockParameter);
            }
        }

        private static void AddControlledParts(ISet<TrackingPart> parts, TrackingPolicyData policy)
        {
            foreach (var part in Parts)
            {
                if (TrackingPolicyUtility.Get(policy, part) != TrackingMode.NoChange)
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

        private sealed class ArbiterLayerStates
        {
            public AnimatorState Idle;
            public AnimatorState Animation;
            public AnimatorState Tracking;
            public AnimatorState Locked;
            public AnimatorState RequestAnimation;
            public AnimatorState RequestTracking;
            public AnimatorState RequestReset;
            public AnimatorState LockRelease;
        }

        /// <summary>Per-part vote candidates and parameters the arbiter's transitions are built from.</summary>
        private sealed class ArbiterInputs
        {
            public List<TrackingVoteCandidate> AnimationCandidates;
            public List<TrackingVoteCandidate> TrackingCandidates;
            public List<string> ActivePoseParameters;
            public string LockParameter;
            public bool HasLock;
            public string ResetParameter;
        }
    }
}
