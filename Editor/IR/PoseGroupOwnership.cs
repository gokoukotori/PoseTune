using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseGroupOwnership
    {
        public static IEnumerable<PoseClip> OwnedClips(PoseGroup group)
        {
            return group == null
                ? Enumerable.Empty<PoseClip>()
                : group.GetComponentsInChildren<PoseClip>(true).Where(clip => IsOwnedBy(group, clip));
        }

        public static bool IsOwnedBy(PoseGroup group, PoseClip clip)
        {
            return group != null &&
                   clip != null &&
                   clip.GetComponentInParent<PoseGroup>(true) == group;
        }
    }
}
