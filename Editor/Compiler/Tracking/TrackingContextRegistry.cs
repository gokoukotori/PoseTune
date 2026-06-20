using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class TrackingContextRegistry
    {
        private readonly List<TrackingContextDefinition> contexts = new();

        public IReadOnlyList<TrackingContextDefinition> Contexts => contexts;

        public int GetOrAdd(TrackingPolicyData policy)
        {
            var copy = policy != null
                ? TrackingPolicyUtility.Copy(policy)
                : TrackingPolicyUtility.NoChange();
            var existing = contexts.FirstOrDefault(context =>
                TrackingPolicyUtility.AreEqual(context.Policy, copy));
            if (existing != null)
            {
                return existing.Id;
            }

            var definition = new TrackingContextDefinition
            {
                Id = contexts.Count + 1,
                Policy = copy
            };
            contexts.Add(definition);
            return definition.Id;
        }
    }

    internal sealed class TrackingContextDefinition
    {
        public int Id { get; set; }
        public TrackingPolicyData Policy { get; set; }
    }
}
