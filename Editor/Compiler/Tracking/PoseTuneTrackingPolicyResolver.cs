using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum TrackingPolicySource
    {
        None,
        ClipPolicy,
        ClipField,
        GroupPolicy,
        RootPolicy,
        KindDefault
    }

    internal sealed class TrackingPolicySummary
    {
        public TrackingPolicySource Source;
        public TrackingPolicyData Policy = TrackingPolicyData.DefaultForPose();
        public Object Context;
        public bool GenerateResetOnExit = true;
    }

    internal sealed class PoseTrackingPolicyResolution
    {
        public TrackingPolicyData Policy = TrackingPolicyData.DefaultForPose();
        public bool GenerateResetOnExit = true;
        public bool HasFullBodyTrackingOverride;
        public TrackingPolicyData FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose();
    }

    internal static class PoseTuneTrackingPolicyResolver
    {
        public static void CollectRootPolicy(PoseTuneRoot root, PoseGraph graph)
        {
            var policies = root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                .Where(policy => policy != null && IsRootLevelPolicy(root, policy))
                .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                .OrderBy(policy => policy.transform == root.transform ? 0 : 1)
                .ThenBy(policy => policy.transform.GetSiblingIndex())
                .ToList();
            graph.RootTrackingPolicyCount = policies.Count;
            var policy = policies.FirstOrDefault();
            if (policy == null)
            {
                return;
            }

            graph.RootTrackingPolicy = TrackingPolicyUtility.Copy(policy.tracking);
            graph.HasCustomRootTrackingPolicy =
                !TrackingPolicyUtility.AreEqual(policy.tracking, TrackingPolicyData.DefaultForPose());
            graph.RootGenerateResetOnExit = policy.generateResetOnExit;
            graph.HasCustomRootGenerateResetOnExit = !policy.generateResetOnExit;
        }

        public static PoseTrackingPolicyResolution ResolvePosePolicy(PoseGraph graph, PoseGroup group, PoseClip clip)
        {
            var result = new PoseTrackingPolicyResolution
            {
                Policy = MergeTrackingPolicy(graph, group, clip),
                GenerateResetOnExit = ResolveGenerateResetOnExit(graph, group, clip)
            };
            result.HasFullBodyTrackingOverride = TryResolveFullBodyTrackingOverride(
                graph,
                group,
                clip,
                out result.FullBodyTrackingPolicy);
            return result;
        }

        public static TrackingPolicySummary ResolveEffectivePolicy(PoseTuneRoot root, Object selectedObject)
        {
            if (root == null)
            {
                return new TrackingPolicySummary { Source = TrackingPolicySource.None };
            }

            if (selectedObject is PoseClip pose)
            {
                var group = pose.GetComponentInParent<PoseGroup>(true);
                if (!pose.emitTrackingControl || (group != null && !group.emitTrackingControl))
                {
                    return Summary(TrackingPolicySource.None, TrackingPolicyUtility.NoChange(), pose, false);
                }

                var clipPolicy = pose.GetComponents<PoseTrackingPolicy>()
                    .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
                if (clipPolicy != null)
                {
                    return Summary(TrackingPolicySource.ClipPolicy, clipPolicy.tracking, clipPolicy,
                        clipPolicy.generateResetOnExit);
                }

                if (TrackingPolicyUtility.WasCustomizedFromPoseDefault(pose.tracking))
                {
                    return Summary(TrackingPolicySource.ClipField, pose.tracking, pose, true);
                }

                var groupPolicy = group != null
                    ? group.GetComponents<PoseTrackingPolicy>().FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled)
                    : null;
                if (groupPolicy != null)
                {
                    return Summary(TrackingPolicySource.GroupPolicy, groupPolicy.tracking, groupPolicy,
                        groupPolicy.generateResetOnExit);
                }

                var rootPolicy = RootPolicy(root);
                if (rootPolicy != null)
                {
                    return Summary(TrackingPolicySource.RootPolicy, rootPolicy.tracking, rootPolicy,
                        rootPolicy.generateResetOnExit);
                }

                return Summary(TrackingPolicySource.KindDefault,
                    TrackingPolicyUtility.DefaultForGroup(group != null ? group.kind : PoseGroupKind.Custom),
                    group != null ? group : root, true);
            }

            if (selectedObject is PoseGroup selectedGroup)
            {
                if (!selectedGroup.emitTrackingControl)
                {
                    return Summary(TrackingPolicySource.None, TrackingPolicyUtility.NoChange(), selectedGroup, false);
                }

                var groupPolicy = selectedGroup.GetComponents<PoseTrackingPolicy>()
                    .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
                if (groupPolicy != null)
                {
                    return Summary(TrackingPolicySource.GroupPolicy, groupPolicy.tracking, groupPolicy,
                        groupPolicy.generateResetOnExit);
                }

                var rootPolicy = RootPolicy(root);
                if (rootPolicy != null)
                {
                    return Summary(TrackingPolicySource.RootPolicy, rootPolicy.tracking, rootPolicy,
                        rootPolicy.generateResetOnExit);
                }

                return Summary(TrackingPolicySource.KindDefault,
                    TrackingPolicyUtility.DefaultForGroup(selectedGroup.kind), selectedGroup, true);
            }

            var policy = RootPolicy(root);
            return policy != null
                ? Summary(TrackingPolicySource.RootPolicy, policy.tracking, policy, policy.generateResetOnExit)
                : Summary(TrackingPolicySource.KindDefault, TrackingPolicyData.DefaultForPose(), root, true);
        }

        public static PoseTrackingPolicy RootPolicy(PoseTuneRoot root)
        {
            return root != null
                ? root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                    .Where(policy => policy != null && IsRootLevelPolicy(root, policy))
                    .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                    .OrderBy(policy => policy.transform == root.transform ? 0 : 1)
                    .ThenBy(policy => policy.transform.GetSiblingIndex())
                    .FirstOrDefault()
                : null;
        }

        private static TrackingPolicySummary Summary(
            TrackingPolicySource source,
            TrackingPolicyData policy,
            Object context,
            bool generateResetOnExit)
        {
            return new TrackingPolicySummary
            {
                Source = source,
                Policy = TrackingPolicyUtility.Copy(policy),
                Context = context,
                GenerateResetOnExit = generateResetOnExit
            };
        }

        private static TrackingPolicyData MergeTrackingPolicy(PoseGraph graph, PoseGroup group, PoseClip clip)
        {
            if (!group.emitTrackingControl || !clip.emitTrackingControl)
            {
                return TrackingPolicyUtility.NoChange();
            }

            var groupPolicy = group.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            var clipPolicy = clip.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (clipPolicy != null)
            {
                return TrackingPolicyUtility.Copy(clipPolicy.tracking);
            }

            if (TrackingPolicyUtility.WasCustomizedFromPoseDefault(clip.tracking))
            {
                return TrackingPolicyUtility.Copy(clip.tracking);
            }

            if (groupPolicy != null)
            {
                return TrackingPolicyUtility.Copy(groupPolicy.tracking);
            }

            return graph.HasCustomRootTrackingPolicy
                ? TrackingPolicyUtility.Copy(graph.RootTrackingPolicy)
                : TrackingPolicyUtility.DefaultForGroup(group.kind);
        }

        private static bool ResolveGenerateResetOnExit(PoseGraph graph, PoseGroup group, PoseClip clip)
        {
            if (!group.emitTrackingControl || !clip.emitTrackingControl)
            {
                return false;
            }

            var clipPolicy = clip.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (clipPolicy != null)
            {
                return clipPolicy.generateResetOnExit;
            }

            var groupPolicy = group.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (groupPolicy != null)
            {
                return groupPolicy.generateResetOnExit;
            }

            return graph.HasCustomRootGenerateResetOnExit ? graph.RootGenerateResetOnExit : true;
        }

        private static bool TryResolveFullBodyTrackingOverride(
            PoseGraph graph,
            PoseGroup group,
            PoseClip clip,
            out TrackingPolicyData tracking)
        {
            if (!group.emitTrackingControl || !clip.emitTrackingControl)
            {
                tracking = TrackingPolicyUtility.NoChange();
                return false;
            }

            var clipPolicy = clip.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (clipPolicy != null && clipPolicy.useFullBodyTrackingOverride)
            {
                tracking = TrackingPolicyUtility.Copy(clipPolicy.fullBodyTracking);
                return true;
            }

            var groupPolicy = group.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (groupPolicy != null && groupPolicy.useFullBodyTrackingOverride)
            {
                tracking = TrackingPolicyUtility.Copy(groupPolicy.fullBodyTracking);
                return true;
            }

            var rootPolicy = graph.RootComponent != null
                ? graph.RootComponent.GetComponentsInChildren<PoseTrackingPolicy>(true)
                    .Where(policy => policy != null && IsRootLevelPolicy(graph.RootComponent, policy))
                    .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                    .OrderBy(policy => policy.transform == graph.RootComponent.transform ? 0 : 1)
                    .ThenBy(policy => policy.transform.GetSiblingIndex())
                    .FirstOrDefault(policy => policy.useFullBodyTrackingOverride)
                : null;
            if (rootPolicy != null)
            {
                tracking = TrackingPolicyUtility.Copy(rootPolicy.fullBodyTracking);
                return true;
            }

            tracking = TrackingPolicyData.DefaultForPose();
            return false;
        }

        private static bool IsRootLevelPolicy(PoseTuneRoot root, PoseTrackingPolicy policy)
        {
            if (policy.transform == root.transform)
            {
                return true;
            }

            return policy.transform.parent == root.transform &&
                   policy.GetComponent<PoseGroup>() == null &&
                   policy.GetComponent<PoseClip>() == null;
        }
    }
}
