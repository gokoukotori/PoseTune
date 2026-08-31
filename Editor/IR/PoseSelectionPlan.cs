using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    [Flags]
    public enum PoseSelectionFallbackReason
    {
        None = 0,
        NonExclusive = 1 << 0,
        NotSynced = 1 << 1,
        ExplicitParameterName = 1 << 2,
        SelectedPoseAuto = 1 << 3
    }

    public sealed class PoseSelectionPlan
    {
        private readonly Dictionary<PoseGroupDefinition, PoseSelectionGroupBinding> groups = new();
        private readonly Dictionary<PoseDefinition, PoseSelectionBinding> poses = new();

        public List<PoseSelectionChannel> Channels = new();
        public IReadOnlyCollection<PoseSelectionGroupBinding> GroupBindings => groups.Values;
        public IReadOnlyCollection<PoseSelectionBinding> PoseBindings => poses.Values;

        public PoseSelectionGroupBinding Find(PoseGroupDefinition group)
        {
            return group != null && groups.TryGetValue(group, out var binding) ? binding : null;
        }

        public PoseSelectionBinding Find(PoseDefinition pose)
        {
            return pose != null && poses.TryGetValue(pose, out var binding) ? binding : null;
        }

        public IReadOnlyList<string> ExclusiveResetParameterNames(
            PoseTuneRoot root,
            IEnumerable<PoseGroupDefinition> candidates,
            PoseGroupDefinition current)
        {
            var currentParameter = Find(current)?.ParameterName ?? "";
            return (candidates ?? Enumerable.Empty<PoseGroupDefinition>())
                .Where(other => other != null &&
                                other != current &&
                                other.Exclusive &&
                                other.Poses.Count > 0 &&
                                PoseTuneCompilerRules.AllowsManualControl(root, other))
                .Select(Find)
                .Where(binding => binding != null &&
                                  !string.IsNullOrWhiteSpace(binding.ParameterName) &&
                                  binding.ParameterName != currentParameter)
                .Select(binding => binding.ParameterName)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        internal PoseSelectionGroupBinding AddGroup(
            PoseSelectionChannel channel,
            PoseGroupDefinition group,
            PoseSelectionFallbackReason fallbackReason)
        {
            var binding = new PoseSelectionGroupBinding
            {
                Channel = channel,
                Group = group,
                FallbackReason = fallbackReason
            };
            groups[group] = binding;
            channel.Groups.Add(group);
            return binding;
        }

        internal void AddPose(PoseSelectionGroupBinding group, PoseDefinition pose, int value)
        {
            var binding = new PoseSelectionBinding
            {
                Group = group,
                Pose = pose,
                Value = value
            };
            poses[pose] = binding;
            group.Poses.Add(binding);
            group.Channel.Poses.Add(binding);
        }
    }

    public sealed class PoseSelectionChannel
    {
        public string ParameterName = "";
        public bool Saved;
        public bool Synced;
        public bool Shared;
        public float DefaultValue;
        public List<PoseGroupDefinition> Groups = new();
        public List<PoseSelectionBinding> Poses = new();
    }

    public sealed class PoseSelectionGroupBinding
    {
        public PoseSelectionChannel Channel;
        public PoseGroupDefinition Group;
        public PoseSelectionFallbackReason FallbackReason;
        public List<PoseSelectionBinding> Poses = new();

        public string ParameterName => Channel?.ParameterName ?? "";
        public bool Shared => Channel?.Shared == true;
    }

    public sealed class PoseSelectionBinding
    {
        public PoseSelectionGroupBinding Group;
        public PoseDefinition Pose;
        public int Value;

        public string ParameterName => Group?.ParameterName ?? "";
    }
}
