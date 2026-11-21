using System.Globalization;
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
        public const string PoseId = "PoseId";
        public const string PoseIdTransient = "PoseIdTransient";
        public const string Height = "Height";
        public const string GeneratedRootName = "PoseTune Generated";

        /// <summary>Prefix of animator-only parameters PoseTune generates; authored names may not use it.</summary>
        public const string InternalParameterPrefix = "PTI/";
        public const string PoseRefreshParameter = InternalParameterPrefix + "PoseRefresh";
        public const string TrackingResetParameterPrefix = InternalParameterPrefix + "TrackingReset/";
        public const string PoseRefreshLayerName = "PT_PoseRefresh";

        public const string ActionWeightLayerName = "PT_ActionWeight";
        public const string ActionWeightInactiveState = "ActionWeightInactive";
        public const string ActionWeightActiveState = "ActionWeightActive";

        public const string CommitExclusivePoseDebugString = "PoseTune Commit Exclusive Pose";

        /// <summary>State names of each <see cref="TrackingArbiterLayerName"/> layer.</summary>
        public static class ArbiterStates
        {
            public const string Idle = "Idle";
            public const string VoteAnimation = "VoteAnimation";
            public const string VoteTracking = "VoteTracking";
            public const string LockedAnimation = "LockedAnimation";
            public const string RequestAnimation = "RequestAnimation";
            public const string RequestTracking = "RequestTracking";
            public const string RequestReset = "RequestReset";
            public const string LockRelease = "LockRelease";
        }

        /// <summary>
        /// <paramref name="localName"/> in the root's parameter namespace, or in the default namespace when there
        /// is no root.
        /// </summary>
        public static string ParameterOf(PoseTuneRoot root, string localName)
        {
            return root != null
                ? root.Parameter(localName)
                : PoseTuneRoot.DefaultParameterNamespace + "/" + localName;
        }

        public static string PoseIdParameter(PoseTuneRoot root, bool saved, int bankNumber)
        {
            var localName = saved ? PoseId : PoseIdTransient;
            if (bankNumber > 1)
            {
                localName += bankNumber.ToString(CultureInfo.InvariantCulture);
            }

            return root.Parameter(localName);
        }

        public static string ShortId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "unknown";
            }

            return id.Length <= 16 ? id : id.Substring(0, 16);
        }

        public static string HeightParameter(PoseTuneRoot root, PoseHeightAdjust height)
        {
            if (height != null && !string.IsNullOrWhiteSpace(height.parameterName))
            {
                return height.parameterName.Trim();
            }

            return ParameterOf(root, Height);
        }

        public static string GroupActiveParameter(PoseGroupDefinition group)
        {
            return GroupActiveParameter(group, PoseClipBlendMode.Override);
        }

        public static string GroupActiveParameter(PoseGroupDefinition group, PoseClipBlendMode blendMode)
        {
            return InternalParameterPrefix + "GroupActive/" + ShortId(group != null ? group.Id : "") + "/" + blendMode;
        }

        public static string PoseActiveParameter(PoseDefinition pose)
        {
            return InternalParameterPrefix + "PoseActive/" + ShortId(pose != null ? pose.Id : "");
        }

        public static string TrackingVoteParameter(PoseGroupDefinition group)
        {
            return InternalParameterPrefix + "TrackingVote/" + Sanitize(group != null ? group.Id : "");
        }

        public static string TrackingResetParameter(TrackingPart part)
        {
            return TrackingResetParameterPrefix + part;
        }

        public static string TrackingArbiterLayerName(TrackingPart part)
        {
            return "PT_TrackingArbiter_" + part;
        }

        /// <summary>The pose-option lock that holds <paramref name="part"/>, or "" when none applies.</summary>
        public static string TrackingArbiterLockParameter(PoseGraph graph, TrackingPart part)
        {
            if (graph?.HasPoseOptions != true || graph.RootComponent == null)
            {
                return "";
            }

            switch (part)
            {
                case TrackingPart.Head:
                    return graph.RootComponent.Parameter(LockHead);
                case TrackingPart.LeftHand:
                case TrackingPart.RightHand:
                case TrackingPart.LeftFingers:
                case TrackingPart.RightFingers:
                    return graph.RootComponent.Parameter(LockHands);
                case TrackingPart.LeftFoot:
                case TrackingPart.RightFoot:
                    return graph.RootComponent.Parameter(LockFeet);
                default:
                    return "";
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var characters = value.Select(character =>
                char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
            return new string(characters);
        }

    }
}
