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
        public bool HasFullBodyTrackingOverride;
        public TrackingPolicyData FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose();
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
            var allPolicies = root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                .Where(policy => policy != null && IsRootLevelPolicy(root, policy))
                .ToList();
            var policies = allPolicies
                .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                .OrderBy(policy => policy.transform == root.transform ? 0 : 1)
                .ThenBy(policy => policy.transform.GetSiblingIndex())
                .ToList();
            graph.RootTrackingPolicyCount = allPolicies.Count;
            var policy = policies.FirstOrDefault();
            if (policy == null)
            {
                return;
            }

            graph.RootTrackingPolicy = TrackingPolicyUtility.Copy(policy.tracking);
            // Component presence is the override marker. A policy whose values happen to
            // equal the defaults is still an explicit authoring decision.
            graph.HasCustomRootTrackingPolicy = true;
            graph.RootGenerateResetOnExit = policy.generateResetOnExit;
            graph.HasCustomRootGenerateResetOnExit = true;
        }

        public static PoseTrackingPolicyResolution ResolvePosePolicy(PoseGraph graph, PoseGroup group, PoseClip clip)
        {
            if (group == null || clip == null || !group.emitTrackingControl || !clip.emitTrackingControl)
            {
                return new PoseTrackingPolicyResolution
                {
                    Policy = TrackingPolicyUtility.NoChange(),
                    GenerateResetOnExit = false,
                    HasFullBodyTrackingOverride = false,
                    FullBodyTrackingPolicy = TrackingPolicyUtility.NoChange()
                };
            }

            var winner = ResolveWinner(graph != null ? graph.RootComponent : null, group, clip);
            var result = new PoseTrackingPolicyResolution
            {
                Policy = TrackingPolicyUtility.Copy(winner.Policy),
                GenerateResetOnExit = winner.GenerateResetOnExit,
                HasFullBodyTrackingOverride = winner.HasFullBodyTrackingOverride,
                FullBodyTrackingPolicy = TrackingPolicyUtility.Copy(winner.FullBodyTrackingPolicy)
            };
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

                var winner = ResolveWinner(root, group, pose);
                return Summary(winner);
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
                    return Summary(ResolvedPolicy.FromComponent(
                        TrackingPolicySource.GroupPolicy,
                        groupPolicy));
                }

                var rootPolicy = RootPolicy(root);
                if (rootPolicy != null)
                {
                    return Summary(ResolvedPolicy.FromComponent(
                        TrackingPolicySource.RootPolicy,
                        rootPolicy));
                }

                return Summary(ResolvedPolicy.FromDefault(selectedGroup.kind, selectedGroup));
            }

            var policy = RootPolicy(root);
            return policy != null
                ? Summary(ResolvedPolicy.FromComponent(TrackingPolicySource.RootPolicy, policy))
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

        private static TrackingPolicySummary Summary(ResolvedPolicy resolved)
        {
            return new TrackingPolicySummary
            {
                Source = resolved.Source,
                Policy = TrackingPolicyUtility.Copy(resolved.Policy),
                Context = resolved.Context,
                GenerateResetOnExit = resolved.GenerateResetOnExit,
                HasFullBodyTrackingOverride = resolved.HasFullBodyTrackingOverride,
                FullBodyTrackingPolicy = TrackingPolicyUtility.Copy(resolved.FullBodyTrackingPolicy)
            };
        }

        private static TrackingPolicySummary Summary(
            TrackingPolicySource source,
            TrackingPolicyData policy,
            Object context,
            bool generateResetOnExit)
        {
            return Summary(new ResolvedPolicy
            {
                Source = source,
                Policy = TrackingPolicyUtility.Copy(policy),
                Context = context,
                GenerateResetOnExit = generateResetOnExit,
                FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose()
            });
        }

        private static ResolvedPolicy ResolveWinner(PoseTuneRoot root, PoseGroup group, PoseClip clip)
        {
            var clipPolicy = clip.GetComponents<PoseTrackingPolicy>()
                .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (clipPolicy != null)
            {
                return ResolvedPolicy.FromComponent(TrackingPolicySource.ClipPolicy, clipPolicy);
            }

            if (TrackingPolicyUtility.WasCustomizedFromPoseDefault(clip.tracking))
            {
                return new ResolvedPolicy
                {
                    Source = TrackingPolicySource.ClipField,
                    Policy = TrackingPolicyUtility.Copy(clip.tracking),
                    Context = clip,
                    GenerateResetOnExit = true,
                    HasFullBodyTrackingOverride = false,
                    FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose()
                };
            }

            var groupPolicy = group != null
                ? group.GetComponents<PoseTrackingPolicy>()
                    .FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled)
                : null;
            if (groupPolicy != null)
            {
                return ResolvedPolicy.FromComponent(TrackingPolicySource.GroupPolicy, groupPolicy);
            }

            var rootPolicy = RootPolicy(root);
            if (rootPolicy != null)
            {
                return ResolvedPolicy.FromComponent(TrackingPolicySource.RootPolicy, rootPolicy);
            }

            return ResolvedPolicy.FromDefault(group != null ? group.kind : PoseGroupKind.Custom,
                group != null ? (Object)group : root);
        }

        private sealed class ResolvedPolicy
        {
            public TrackingPolicySource Source;
            public TrackingPolicyData Policy;
            public Object Context;
            public bool GenerateResetOnExit;
            public bool HasFullBodyTrackingOverride;
            public TrackingPolicyData FullBodyTrackingPolicy;

            public static ResolvedPolicy FromComponent(TrackingPolicySource source, PoseTrackingPolicy component)
            {
                return new ResolvedPolicy
                {
                    Source = source,
                    Policy = TrackingPolicyUtility.Copy(component.tracking),
                    Context = component,
                    GenerateResetOnExit = component.generateResetOnExit,
                    HasFullBodyTrackingOverride = component.useFullBodyTrackingOverride,
                    FullBodyTrackingPolicy = TrackingPolicyUtility.Copy(component.fullBodyTracking)
                };
            }

            public static ResolvedPolicy FromDefault(PoseGroupKind kind, Object context)
            {
                return new ResolvedPolicy
                {
                    Source = TrackingPolicySource.KindDefault,
                    Policy = TrackingPolicyUtility.DefaultForGroup(kind),
                    Context = context,
                    GenerateResetOnExit = true,
                    HasFullBodyTrackingOverride = false,
                    FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose()
                };
            }
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
