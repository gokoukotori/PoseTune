using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneNames
    {
        public const string Mode = "Mode";
        public const string LockHead = "LockHead";
        public const string LockHands = "LockHands";
        public const string LockFeet = "LockFeet";
        public const string LocomotionLock = "LocomotionLock";
        public const string SupineFlag = "SupineFlag";
        public const string GeneratedRootName = "PoseTune Generated";

        public static string ShortGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "unknown";
            }

            return guid.Length <= 8 ? guid : guid.Substring(0, 8);
        }

        public static string HeightParameter(PoseTuneRoot root, PoseHeightAdjust height)
        {
            if (height != null && !string.IsNullOrWhiteSpace(height.parameterName))
            {
                return height.parameterName.Trim();
            }

            return root != null ? root.Parameter("Height") : "PT/Height";
        }

        public static string GroupActiveParameter(PoseGroupDefinition group)
        {
            return GroupActiveParameter(group, PoseClipBlendMode.Override);
        }

        public static string GroupActiveParameter(PoseGroupDefinition group, PoseClipBlendMode blendMode)
        {
            return "PTI/GroupActive/" + ShortGuid(group != null ? group.Id : "") + "/" + blendMode;
        }

        public static string PoseActiveParameter(PoseDefinition pose)
        {
            return "PTI/PoseActive/" + ShortGuid(pose != null ? pose.Id : "");
        }
    }

    internal sealed class PoseLayerBucketDefinition
    {
        public string LayerName;
        public PoseClipBlendMode BlendMode;
        public List<PoseDefinition> Poses = new();
    }

    internal static class PoseTuneLayerNaming
    {
        public static List<PoseLayerBucketDefinition> LayerBuckets(PoseGroupDefinition group)
        {
            if (group == null || group.Poses.Count == 0)
            {
                return new List<PoseLayerBucketDefinition>
                {
                    new()
                    {
                        LayerName = group != null ? group.LayerName : "",
                        BlendMode = PoseClipBlendMode.Override,
                        Poses = new List<PoseDefinition>()
                    }
                };
            }

            var modes = group.Poses.Select(pose => pose.BlendMode).Distinct().ToList();
            if (modes.Count == 1)
            {
                return new List<PoseLayerBucketDefinition>
                {
                    new()
                    {
                        LayerName = group.LayerName,
                        BlendMode = modes[0],
                        Poses = group.Poses.ToList()
                    }
                };
            }

            var buckets = new List<PoseLayerBucketDefinition>();
            foreach (var mode in new[] { PoseClipBlendMode.Override, PoseClipBlendMode.Additive })
            {
                var poses = group.Poses.Where(pose => pose.BlendMode == mode).ToList();
                if (poses.Count == 0)
                {
                    continue;
                }

                buckets.Add(new PoseLayerBucketDefinition
                {
                    LayerName = group.LayerName + "_" + mode,
                    BlendMode = mode,
                    Poses = poses
                });
            }

            return buckets;
        }

        public static IEnumerable<string> ExpectedLayerNames(PoseGroupDefinition group)
        {
            return LayerBuckets(group).Select(bucket => bucket.LayerName);
        }

        public static IEnumerable<string> GroupActiveParameters(PoseGroupDefinition group)
        {
            return LayerBuckets(group).Select(bucket => PoseTuneNames.GroupActiveParameter(group, bucket.BlendMode));
        }
    }
}
