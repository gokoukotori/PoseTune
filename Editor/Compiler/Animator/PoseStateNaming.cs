using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateNaming
    {
        /// <summary>Suffix of the desktop-only variant of a VR base state.</summary>
        public const string DesktopSuffix = "_Desktop";
        /// <summary>Suffix of the VR variant of a desktop-only pose space base state.</summary>
        public const string VrSuffix = "_VR";
        /// <summary>Suffix of the full body tracking variant.</summary>
        public const string FullBodySuffix = "_FBT";
        public const string HandoffPrefix = "Handoff_";
        public const string CommitExclusivePrefix = "CommitExclusive_";

        public static string Name(PoseDefinition pose, HashSet<string> duplicateBaseNames, string suffix = "")
        {
            var baseName = BaseName(pose);
            if (duplicateBaseNames != null && duplicateBaseNames.Contains(baseName))
            {
                baseName += "_" + PoseTuneNames.ShortId(pose.Id);
            }

            return baseName + suffix;
        }

        public static HashSet<string> DuplicateBaseNames(IEnumerable<PoseDefinition> poses)
        {
            return poses
                .GroupBy(BaseName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();
        }

        public static string CleanupName(
            PoseDefinition pose,
            HashSet<string> duplicateBaseNames,
            string suffix = "")
        {
            return HandoffPrefix + Name(pose, duplicateBaseNames, suffix);
        }

        private static string BaseName(PoseDefinition pose)
        {
            var baseName = pose?.SourceMotion is BlendTree && !string.IsNullOrWhiteSpace(pose.DisplayName)
                ? pose.DisplayName
                : pose?.Clip != null && !string.IsNullOrWhiteSpace(pose.Clip.name)
                    ? pose.Clip.name
                    : pose?.SourceMotion != null && !string.IsNullOrWhiteSpace(pose.SourceMotion.name)
                        ? pose.SourceMotion.name
                        : pose != null && !string.IsNullOrWhiteSpace(pose.DisplayName)
                            ? pose.DisplayName
                            : "";
            return string.IsNullOrWhiteSpace(baseName) ? "Pose" : baseName;
        }
    }
}
