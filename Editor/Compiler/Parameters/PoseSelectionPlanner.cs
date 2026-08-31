using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseSelectionPlanner
    {
        public static PoseSelectionPlan Build(PoseGraph graph)
        {
            var plan = new PoseSelectionPlan();
            if (graph?.RootComponent == null)
            {
                return plan;
            }

            var root = graph.RootComponent;
            var groups = PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => PoseTuneCompilerRules.RequiresPoseSelectionParameter(root, group))
                .ToList();
            if (root.poseSelectionSyncMode != PoseSelectionSyncMode.SharedExclusivePoseId)
            {
                foreach (var group in groups)
                {
                    AddDedicated(plan, root, group, PoseSelectionFallbackReason.None);
                }

                return plan;
            }

            var shared = new List<PoseGroupDefinition>();
            var dedicated = new List<(PoseGroupDefinition Group, PoseSelectionFallbackReason Reason)>();
            foreach (var group in groups)
            {
                var reason = FallbackReason(root, group);
                if (reason == PoseSelectionFallbackReason.None)
                {
                    shared.Add(group);
                }
                else
                {
                    dedicated.Add((group, reason));
                }
            }

            AddSharedBank(plan, root, shared.Where(group => group.Saved), saved: true);
            AddSharedBank(plan, root, shared.Where(group => !group.Saved), saved: false);
            foreach (var item in dedicated)
            {
                AddDedicated(plan, root, item.Group, item.Reason);
            }

            return plan;
        }

        private static void AddSharedBank(
            PoseSelectionPlan plan,
            PoseTuneRoot root,
            IEnumerable<PoseGroupDefinition> source,
            bool saved)
        {
            var groups = source
                .OrderBy(group => group.MenuOrder)
                .ThenBy(group => group.Id, StringComparer.Ordinal)
                .ToList();
            if (groups.Count == 0)
            {
                return;
            }

            var channel = new PoseSelectionChannel
            {
                ParameterName = root.Parameter(saved ? PoseTuneNames.PoseId : PoseTuneNames.PoseIdTransient),
                Saved = saved,
                Synced = true,
                Shared = true
            };
            plan.Channels.Add(channel);

            var nextValue = 1;
            foreach (var group in groups)
            {
                var groupBinding = plan.AddGroup(channel, group, PoseSelectionFallbackReason.None);
                foreach (var pose in group.Poses
                             .OrderBy(pose => pose.MenuOrder)
                             .ThenBy(pose => pose.Id, StringComparer.Ordinal))
                {
                    plan.AddPose(groupBinding, pose, nextValue++);
                }
            }

            channel.DefaultValue = channel.Poses.FirstOrDefault(binding => binding.Pose.Initial)?.Value ?? 0;
        }

        private static void AddDedicated(
            PoseSelectionPlan plan,
            PoseTuneRoot root,
            PoseGroupDefinition group,
            PoseSelectionFallbackReason reason)
        {
            var channel = new PoseSelectionChannel
            {
                ParameterName = group.ParameterName,
                Saved = group.Saved,
                Synced = group.Synced,
                Shared = false,
                DefaultValue = group.Poses.FirstOrDefault(pose => pose.Initial)?.SelectionValue(root) ?? 0
            };
            plan.Channels.Add(channel);
            var groupBinding = plan.AddGroup(channel, group, reason);
            foreach (var pose in group.Poses)
            {
                plan.AddPose(groupBinding, pose, pose.SelectionValue(root));
            }
        }

        private static PoseSelectionFallbackReason FallbackReason(
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            var reason = PoseSelectionFallbackReason.None;
            if (!group.Exclusive)
            {
                reason |= PoseSelectionFallbackReason.NonExclusive;
            }

            if (!group.Synced)
            {
                reason |= PoseSelectionFallbackReason.NotSynced;
            }

            if (group.Source != null && !string.IsNullOrWhiteSpace(group.Source.parameterName))
            {
                reason |= PoseSelectionFallbackReason.ExplicitParameterName;
            }

            if (PoseTuneCompilerRules.UsesSelectedPoseAutoSelection(root, group))
            {
                reason |= PoseSelectionFallbackReason.SelectedPoseAuto;
            }

            return reason;
        }
    }
}
