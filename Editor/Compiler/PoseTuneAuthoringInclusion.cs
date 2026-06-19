using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAuthoringInclusion
    {
        public static bool ComponentEnabled(Component component)
        {
            return component != null && (component is not Behaviour behaviour || behaviour.enabled);
        }

        public static bool Includes(PoseGroup group)
        {
            return group != null && group.includeInBuild && ComponentEnabled(group);
        }

        public static bool Includes(PoseClip clip)
        {
            return clip != null && clip.includeInBuild && ComponentEnabled(clip);
        }

        public static bool Includes(PoseHeightAdjust height)
        {
            return height != null && height.includeInBuild && ComponentEnabled(height);
        }
    }
}
