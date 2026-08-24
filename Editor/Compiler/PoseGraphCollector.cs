using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseGraphCollector
    {
        public PoseGraph Collect(PoseTuneRoot root)
        {
            var options = CollectOptions(root, out var hasPoseOptions);
            var goroneCompatibilities = CollectGoroneSystemExCompatibilities(root);
            var heightAdjusts = CollectHeightAdjusts(root);
            var graph = new PoseGraph
            {
                RootComponent = root,
                AvatarDescriptor = root != null ? root.GetComponentInParent<VRCAvatarDescriptor>(true) : null,
                Menu = root != null
                    ? root.GetComponentsInChildren<PoseMenu>(true)
                        .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled)
                    : null,
                HeightAdjust = heightAdjusts.FirstOrDefault(),
                HeightAdjustCount = heightAdjusts.Count,
                GoroneSystemExCompatibility = goroneCompatibilities.FirstOrDefault(),
                GoroneSystemExCompatibilityCount = goroneCompatibilities.Count,
                Options = options,
                HasPoseOptions = hasPoseOptions
            };
            graph.AvatarRoot = graph.AvatarDescriptor != null ? graph.AvatarDescriptor.gameObject : null;

            if (root == null)
            {
                return graph;
            }

            PoseTuneTrackingPolicyResolver.CollectRootPolicyCount(root, graph);

            var usedParameters = new HashSet<string>();
            var usedLayerNames = new HashSet<string>();
            var groups = root.GetComponentsInChildren<PoseGroup>(true)
                .Where(PoseTuneAuthoringInclusion.Includes)
                .OrderBy(g => g.menuOrder)
                .ThenBy(g => g.displayName)
                .ThenBy(g => g.name)
                .ToList();

            foreach (var group in groups)
            {
                var groupDefinition = PoseGroupCollector.Collect(root, graph, group, usedParameters, usedLayerNames);
                graph.Groups.Add(groupDefinition);
                graph.Poses.AddRange(groupDefinition.Poses);
            }

            return graph;
        }

        private static PoseTuneOptions CollectOptions(PoseTuneRoot root, out bool hasPoseOptions)
        {
            var source = root != null
                ? root.GetComponentsInChildren<PoseOption>(true)
                    .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled)
                : null;
            hasPoseOptions = source != null;
            if (source == null || source.options == null)
            {
                return new PoseTuneOptions();
            }

            return new PoseTuneOptions
            {
                lockHead = source.options.lockHead,
                lockHands = source.options.lockHands,
                lockFeet = source.options.lockFeet,
                locomotionLock = source.options.locomotionLock
            };
        }

        private static List<PoseTuneGoroneSystemExCompatibility> CollectGoroneSystemExCompatibilities(PoseTuneRoot root)
        {
            return root != null
                ? root.GetComponentsInChildren<PoseTuneGoroneSystemExCompatibility>(true)
                    .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                    .Where(component => component.guardMode != GoroneSystemExGuardMode.Disabled)
                    .OrderBy(component => component.transform == root.transform ? 0 : 1)
                    .ThenBy(component => component.transform.GetSiblingIndex())
                    .ToList()
                : new List<PoseTuneGoroneSystemExCompatibility>();
        }

        private static List<PoseHeightAdjust> CollectHeightAdjusts(PoseTuneRoot root)
        {
            return root != null
                ? root.GetComponentsInChildren<PoseHeightAdjust>(true)
                    .Where(PoseTuneAuthoringInclusion.Includes)
                    .ToList()
                : new List<PoseHeightAdjust>();
        }
    }
}
