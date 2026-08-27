using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseStateNaming
    {
        public static string Name(PoseDefinition pose)
        {
            return Name(pose, null, "");
        }

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
            return "Handoff_" + Name(pose, duplicateBaseNames, suffix);
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
