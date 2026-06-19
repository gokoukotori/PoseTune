using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseGraphBuildFilter
    {
        public static IEnumerable<PoseGroupDefinition> BuildableGroups(PoseGraph graph)
        {
            return graph?.Groups?.Where(group => group.Poses.Count > 0) ?? Enumerable.Empty<PoseGroupDefinition>();
        }
    }
}
