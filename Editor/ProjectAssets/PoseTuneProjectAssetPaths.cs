using System;
using System.IO;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneProjectAssetPaths
    {
        public static string BakeRootPath(PoseGraph graph)
        {
            var avatarName = Sanitize(graph?.AvatarRoot != null ? graph.AvatarRoot.name : "Avatar");
            var rootGuid = graph?.RootComponent != null
                ? PoseTuneNames.ShortGuid(graph.RootComponent.StableGuid)
                : "unknown";
            return "Assets/PoseTuneGenerated/" + avatarName + "/" + rootGuid;
        }

        private static string Sanitize(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "Avatar" : value;
        }
    }
}
