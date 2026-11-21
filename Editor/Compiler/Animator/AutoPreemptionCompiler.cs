using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    /// <summary>
    /// Post-pass over a group's finished pose layers: lets a higher-priority auto pose preempt a lower-priority one
    /// by copying the higher pose's auto-entry conditions onto exits from the lower pose's states to their handoff.
    /// </summary>
    internal static class AutoPreemptionCompiler
    {
        internal static void AddHigherPriorityAutoPreemptionTransitions(
            AnimatorController controller,
            PoseGraph graph,
            PoseGroupDefinition group)
        {
            if (graph?.RootComponent == null ||
                !graph.RootComponent.enableAutoContextSwitch ||
                group == null ||
                group.ActivationMode == PoseGroupActivationMode.Manual)
            {
                return;
            }

            var records = AutoPreemptionRecords(controller, graph, group);
            var modeParameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var voteParameter = PoseTuneNames.TrackingVoteParameter(group);
            var activeParameters = new HashSet<string>(PoseTuneLayerNaming.GroupActiveParameters(group));
            // Unity returns copied arrays for transitions and conditions. Read them once per layer.
            var entriesByLayer = records.Select(record => record.Layer).Distinct().ToDictionary(
                layer => layer,
                layer => layer.stateMachine.anyStateTransitions
                    .Where(transition => transition != null && transition.destinationState != null)
                    .Select(transition => new
                    {
                        State = transition.destinationState,
                        Conditions = transition.conditions
                    })
                    .Where(entry => entry.Conditions.Any(condition =>
                        condition.parameter == modeParameter &&
                        condition.mode == AnimatorConditionMode.Equals &&
                        Mathf.Approximately(condition.threshold, PoseTuneModeValues.Auto)))
                    .Select(entry => new AutoPreemptionEntry
                    {
                        State = entry.State,
                        Conditions = entry.Conditions.Where(condition =>
                            !ExcludedPreemptionCondition(condition, voteParameter, activeParameters)).ToArray()
                    }).ToList());
            for (var lowerIndex = 1; lowerIndex < records.Count; lowerIndex++)
            {
                var higherRecords = records.Take(lowerIndex).ToList();
                var higherStates = new HashSet<AnimatorState>(higherRecords
                    .SelectMany(record => record.Variants)
                    .Select(pair => pair.State));
                var higherAutoEntries = records
                    .Take(lowerIndex)
                    .Select(record => record.Layer)
                    .Distinct()
                    .SelectMany(layer => entriesByLayer[layer])
                    .Where(entry => higherStates.Contains(entry.State))
                    .Select(entry => entry.Conditions);
                var uniqueConditionSets = UniquePreemptionConditionSets(higherAutoEntries);

                foreach (var pair in records[lowerIndex].Variants)
                {
                    foreach (var conditions in uniqueConditionSets)
                    {
                        var preempt = AnimatorTransitionFactory.Immediate(pair.State, pair.Handoff);
                        preempt.conditions = conditions;
                    }
                }
            }
        }

        private static List<AnimatorCondition[]> UniquePreemptionConditionSets(
            IEnumerable<AnimatorCondition[]> entries)
        {
            var result = new List<AnimatorCondition[]>();
            foreach (var conditions in entries)
            {
                if (result.Any(existing => SameConditionMultiset(existing, conditions)))
                {
                    continue;
                }

                result.Add(conditions);
            }

            return result;
        }

        private static bool ExcludedPreemptionCondition(
            AnimatorCondition condition,
            string voteParameter,
            HashSet<string> activeParameters)
        {
            return (condition.parameter == voteParameter &&
                    condition.mode == AnimatorConditionMode.Equals &&
                    Mathf.Approximately(condition.threshold, 0f)) ||
                   (activeParameters.Contains(condition.parameter) &&
                    condition.mode == AnimatorConditionMode.Less &&
                    Mathf.Approximately(condition.threshold, ActiveParameterConditions.Threshold));
        }

        private static bool SameConditionMultiset(
            IReadOnlyList<AnimatorCondition> left,
            IReadOnlyList<AnimatorCondition> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var matched = new bool[right.Count];
            foreach (var expected in left)
            {
                var found = false;
                for (var index = 0; index < right.Count; index++)
                {
                    if (matched[index] ||
                        !string.Equals(expected.parameter, right[index].parameter, System.StringComparison.Ordinal) ||
                        expected.mode != right[index].mode ||
                        !expected.threshold.Equals(right[index].threshold))
                    {
                        continue;
                    }

                    matched[index] = true;
                    found = true;
                    break;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<PoseLayerRecord> AutoPreemptionRecords(
            AnimatorController controller,
            PoseGraph graph,
            PoseGroupDefinition group)
        {
            var buckets = PoseTuneLayerNaming.LayerBuckets(group);
            var layers = controller.layers;
            var layersByBucket = buckets.ToDictionary(bucket => bucket,
                bucket => layers.Last(candidate => candidate.name == bucket.LayerName));
            var duplicatesByBucket = buckets.ToDictionary(bucket => bucket,
                bucket => PoseStateNaming.DuplicateBaseNames(bucket.Poses));
            var statesByBucket = buckets.ToDictionary(bucket => bucket,
                bucket => layersByBucket[bucket].stateMachine.states
                    .Select(child => child.state)
                    .Where(state => state != null)
                    .GroupBy(state => state.name)
                    .ToDictionary(states => states.Key, states => states.First()));
            var result = new List<PoseLayerRecord>();
            foreach (var pose in PoseLayerCompiler.PosesForTransitionGeneration(group.Poses))
            {
                var bucket = buckets.First(candidate => candidate.Poses.Contains(pose));
                var layer = layersByBucket[bucket];
                var duplicates = duplicatesByBucket[bucket];
                var states = statesByBucket[bucket];
                var variants = new List<PoseVariantHandoff>();
                foreach (var planned in PoseStateVariantRules.Plan(graph.RootComponent, group, pose).All)
                {
                    AddVariantHandoff(
                        states,
                        variants,
                        PoseStateNaming.Name(pose, duplicates, planned.Suffix),
                        PoseStateNaming.CleanupName(pose, duplicates, planned.Suffix));
                }

                result.Add(new PoseLayerRecord
                {
                    Layer = layer,
                    Variants = variants
                });
            }

            return result;
        }

        private static void AddVariantHandoff(
            IReadOnlyDictionary<string, AnimatorState> states,
            ICollection<PoseVariantHandoff> variants,
            string stateName,
            string handoffName)
        {
            states.TryGetValue(stateName, out var state);
            states.TryGetValue(handoffName, out var handoff);
            if (state != null && handoff != null)
            {
                variants.Add(new PoseVariantHandoff(state, handoff));
            }
        }

        private sealed class AutoPreemptionEntry
        {
            public AnimatorState State;
            public AnimatorCondition[] Conditions;
        }

        private sealed class PoseLayerRecord
        {
            public AnimatorControllerLayer Layer;
            public List<PoseVariantHandoff> Variants;
        }

        private readonly struct PoseVariantHandoff
        {
            public PoseVariantHandoff(AnimatorState state, AnimatorState handoff)
            {
                State = state;
                Handoff = handoff;
            }

            public AnimatorState State { get; }
            public AnimatorState Handoff { get; }
        }
    }
}
