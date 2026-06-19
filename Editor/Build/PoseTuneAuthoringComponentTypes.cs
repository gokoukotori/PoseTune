using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAuthoringComponentTypes
    {
        public static bool IsAuthoringComponent(Component component)
        {
            return component is PoseTuneRoot or PoseTuneAssistant or PoseGroup or PoseClip
                or PoseCondition or PoseTrackingPolicy or PoseMenu or PoseHeightAdjust or PoseOverrideImport
                or PoseOption or PoseTuneGoroneSystemExCompatibility;
        }

        public static bool IsGeneratedMarker(Component component)
        {
            return component is PoseTuneGeneratedMarker;
        }

        public static bool IsRemovableBuildComponent(Component component)
        {
            return IsGeneratedMarker(component) ||
                   (IsAuthoringComponent(component) && component is not PoseTuneRoot);
        }
    }
}
