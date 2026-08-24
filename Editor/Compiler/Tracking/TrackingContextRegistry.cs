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
            TrackingPolicyData policy)
        {
            if (policy == null || TrackingPolicyUtility.IsNoChange(policy))
            {
                return 0;
            }

            var copy = TrackingPolicyUtility.Copy(policy);
            var existing = votes.FirstOrDefault(vote =>
                vote.GroupId == (group?.Id ?? "") &&
                TrackingPolicyUtility.AreEqual(vote.Policy, copy));
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
        public TrackingPolicyData Policy { get; set; }
    }
}
