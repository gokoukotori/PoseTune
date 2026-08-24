using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum TrackingPolicySource
    {
        None,
        GroupPolicy,
        RootPolicy,
        KindDefault
    }

    internal sealed class ResolvedGroupTrackingPolicy
    {
        public TrackingPolicySource Source;
        public TrackingPolicyData Policy = TrackingPolicyUtility.NoChange();
        public Object Context;
        public bool GenerateResetOnExit;
        public bool HasFullBodyTrackingOverride;
        public TrackingPolicyData FullBodyTrackingPolicy = TrackingPolicyUtility.NoChange();
    }

    internal static class PoseTuneTrackingPolicyResolver
    {
        public static void CollectRootPolicyCount(PoseTuneRoot root, PoseGraph graph)
        {
            if (graph != null)
            {
                graph.RootTrackingPolicyCount = RootPolicies(root, false).Count;
            }
        }

        public static ResolvedGroupTrackingPolicy ResolveGroupPolicy(PoseTuneRoot root, PoseGroup group)
        {
            if (group == null || !group.emitTrackingControl)
            {
                return Disabled(group);
            }

            var groupPolicy = GroupPolicy(group);
            if (groupPolicy != null)
            {
                return FromComponent(TrackingPolicySource.GroupPolicy, groupPolicy);
            }

            var rootPolicy = RootPolicy(root);
            if (rootPolicy != null)
            {
                return FromComponent(TrackingPolicySource.RootPolicy, rootPolicy);
            }

            return FromDefault(group.kind, group);
        }

        public static ResolvedGroupTrackingPolicy ResolveEffectivePolicy(PoseTuneRoot root, Object selectedObject)
        {
            if (root == null)
            {
                return Disabled(null);
            }

            if (selectedObject is PoseClip pose)
            {
                return ResolveGroupPolicy(root, pose.GetComponentInParent<PoseGroup>(true));
            }

            if (selectedObject is PoseGroup group)
            {
                return ResolveGroupPolicy(root, group);
            }

            var policy = RootPolicy(root);
            return policy != null
                ? FromComponent(TrackingPolicySource.RootPolicy, policy)
                : new ResolvedGroupTrackingPolicy
                {
                    Source = TrackingPolicySource.KindDefault,
                    Policy = TrackingPolicyData.DefaultForPose(),
                    Context = root,
                    GenerateResetOnExit = true,
                    HasFullBodyTrackingOverride = false,
                    FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose()
                };
        }

        public static PoseTrackingPolicy GroupPolicy(PoseGroup group, bool includeDisabled = false)
        {
            if (group == null)
            {
                return null;
            }

            return group.GetComponents<PoseTrackingPolicy>()
                .Where(policy => policy != null)
                .Where(policy => includeDisabled || PoseTuneAuthoringInclusion.ComponentEnabled(policy))
                .FirstOrDefault();
        }

        public static PoseTrackingPolicy RootPolicy(PoseTuneRoot root)
        {
            return RootPolicies(root, false).FirstOrDefault();
        }

        public static IReadOnlyList<PoseTrackingPolicy> RootPolicies(PoseTuneRoot root, bool includeDisabled)
        {
            return AllPolicies(root)
                .Where(policy => IsRootLevelPolicy(root, policy))
                .Where(policy => includeDisabled || PoseTuneAuthoringInclusion.ComponentEnabled(policy))
                .OrderBy(policy => policy.transform == root.transform ? 0 : 1)
                .ThenBy(policy => policy.transform.GetSiblingIndex())
                .ToList();
        }

        public static IReadOnlyList<PoseTrackingPolicy> UnsupportedPolicies(PoseTuneRoot root)
        {
            return AllPolicies(root)
                .Where(PoseTuneAuthoringInclusion.ComponentEnabled)
                .Where(policy => !IsRootLevelPolicy(root, policy) && policy.GetComponent<PoseGroup>() == null)
                .ToList();
        }

        public static bool IsRootLevelPolicy(PoseTuneRoot root, PoseTrackingPolicy policy)
        {
            if (root == null || policy == null || NearestRoot(policy) != root)
            {
                return false;
            }

            if (policy.transform == root.transform)
            {
                return true;
            }

            return policy.transform.parent == root.transform &&
                   policy.GetComponent<PoseGroup>() == null &&
                   policy.GetComponent<PoseClip>() == null;
        }

        private static IEnumerable<PoseTrackingPolicy> AllPolicies(PoseTuneRoot root)
        {
            return root != null
                ? root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                    .Where(policy => policy != null && NearestRoot(policy) == root)
                : Enumerable.Empty<PoseTrackingPolicy>();
        }

        private static PoseTuneRoot NearestRoot(Component component)
        {
            return component != null ? component.GetComponentInParent<PoseTuneRoot>(true) : null;
        }

        private static ResolvedGroupTrackingPolicy Disabled(Object context)
        {
            return new ResolvedGroupTrackingPolicy
            {
                Source = TrackingPolicySource.None,
                Policy = TrackingPolicyUtility.NoChange(),
                Context = context,
                GenerateResetOnExit = false,
                HasFullBodyTrackingOverride = false,
                FullBodyTrackingPolicy = TrackingPolicyUtility.NoChange()
            };
        }

        private static ResolvedGroupTrackingPolicy FromComponent(
            TrackingPolicySource source,
            PoseTrackingPolicy component)
        {
            return new ResolvedGroupTrackingPolicy
            {
                Source = source,
                Policy = TrackingPolicyUtility.Copy(component.tracking),
                Context = component,
                GenerateResetOnExit = component.generateResetOnExit,
                HasFullBodyTrackingOverride = component.useFullBodyTrackingOverride,
                FullBodyTrackingPolicy = TrackingPolicyUtility.Copy(component.fullBodyTracking)
            };
        }

        private static ResolvedGroupTrackingPolicy FromDefault(PoseGroupKind kind, Object context)
        {
            return new ResolvedGroupTrackingPolicy
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
}
