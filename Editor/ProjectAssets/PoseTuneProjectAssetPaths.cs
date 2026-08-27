using System;
using System.IO;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneProjectAssetPaths
    {
        public static bool TryGetBakeRootPath(PoseGraph graph, out string path)
        {
            path = "";
            if (graph?.RootComponent == null ||
                !PoseTuneObjectIdentity.TryGetPersistentHash(graph.RootComponent, out var rootHash))
            {
                return false;
            }

            var avatarName = Sanitize(graph?.AvatarRoot != null ? graph.AvatarRoot.name : "Avatar");
            path = "Assets/PoseTuneGenerated/" + avatarName + "/" + rootHash;
            return true;
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
