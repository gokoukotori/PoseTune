using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum TrackingPart
    {
        Head,
        LeftHand,
        RightHand,
        Hip,
        LeftFoot,
        RightFoot,
        LeftFingers,
        RightFingers,
        Eyes,
        Mouth
    }

    internal sealed class TrackingVoteRegistry
    {
        private readonly List<TrackingVoteDefinition> votes = new();

        public IReadOnlyList<TrackingVoteDefinition> Votes => votes;

        public int GetOrAdd(
            PoseGroupDefinition group,
            PoseDefinition pose,
            string variant,
            TrackingPolicyData policy)
        {
            var copy = policy != null
                ? TrackingPolicyUtility.Copy(policy)
                : TrackingPolicyUtility.NoChange();
            var existing = votes.FirstOrDefault(vote =>
                vote.GroupId == (group?.Id ?? "") &&
                vote.PoseId == (pose?.Id ?? "") &&
                vote.Variant == (variant ?? ""));
            if (existing != null)
            {
                return existing.Id;
            }

            var definition = new TrackingVoteDefinition
            {
                Id = votes.Where(vote => vote.GroupId == (group?.Id ?? ""))
                    .Select(vote => vote.Id)
                    .DefaultIfEmpty(0)
                    .Max() + 1,
                GroupId = group?.Id ?? "",
                PoseId = pose?.Id ?? "",
                Variant = variant ?? "",
                Policy = copy
            };
            votes.Add(definition);
            return definition.Id;
        }
    }

    internal sealed class TrackingVoteDefinition
    {
        public int Id { get; set; }
        public string GroupId { get; set; }
        public string PoseId { get; set; }
        public string Variant { get; set; }
        public TrackingPolicyData Policy { get; set; }
    }
}
