using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseNameAllocator
    {
        public static void AssignNames(
            PoseTuneRoot root,
            PoseGroup group,
            PoseGroupDefinition definition,
            HashSet<string> usedParameters,
            HashSet<string> usedLayerNames)
        {
            var buildable = definition.Poses.Count > 0;
            definition.LayerName = buildable
                ? NormalizeLayerName(group, usedLayerNames)
                : LayerBaseName(group);
            definition.ParameterName = buildable && PoseTuneCompilerRules.AllowsManualControl(root, definition)
                ? NormalizeGroupParameter(root, group, usedParameters)
                : GroupParameterName(root, group);
        }

        private static string NormalizeLayerName(PoseGroup group, HashSet<string> usedLayerNames)
        {
            var baseName = LayerBaseName(group);
            if (usedLayerNames.Add(baseName))
            {
                return baseName;
            }

            var suffix = PoseTuneNames.ShortGuid(group.StableGuid);
            var layerName = baseName + "_" + suffix;
            var index = 2;
            while (!usedLayerNames.Add(layerName))
            {
                layerName = baseName + "_" + suffix + "_" + index++;
            }

            return layerName;
        }

        private static string LayerBaseName(PoseGroup group)
        {
            var segment = group.kind == PoseGroupKind.Custom
                ? Slug(string.IsNullOrWhiteSpace(group.displayName) ? group.name : group.displayName)
                : group.kind.ToString();
            if (string.IsNullOrWhiteSpace(segment))
            {
                segment = group.kind.ToString();
            }

            return "PT_" + segment;
        }

        private static string NormalizeGroupParameter(PoseTuneRoot root, PoseGroup group, HashSet<string> usedParameters)
        {
            var baseName = GroupParameterName(root, group);
            var parameter = baseName;
            var suffix = 2;
            while (!usedParameters.Add(parameter))
            {
                parameter = baseName + "_" + suffix++;
            }

            return parameter;
        }

        private static string GroupParameterName(PoseTuneRoot root, PoseGroup group)
        {
            var parameterSegment = group.kind == PoseGroupKind.Custom
                ? Slug(group.name)
                : group.kind.ToString();
            if (string.IsNullOrWhiteSpace(parameterSegment))
            {
                parameterSegment = "Custom_" + PoseTuneNames.ShortGuid(group.StableGuid);
            }

            return string.IsNullOrWhiteSpace(group.parameterName)
                ? root.Parameter(parameterSegment)
                : group.parameterName.Trim();
        }

        private static string Slug(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Custom"
                : new string(value.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '/').ToArray());
        }
    }
}
