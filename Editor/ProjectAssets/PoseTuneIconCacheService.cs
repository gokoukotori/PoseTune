using System.IO;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneIconCacheService
    {
        public string IconsFolder(PoseGraph graph)
        {
            return PoseTuneProjectAssetPaths.BakeRootPath(graph) + "/Icons";
        }

        public Texture2D LoadCachedThumbnail(PoseGraph graph, PoseDefinition pose)
        {
            if (pose?.Source == null)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(ThumbnailAssetPath(pose.Source, IconsFolder(graph)));
        }

        public string ThumbnailAssetPath(PoseClip pose, string folder)
        {
            return folder.TrimEnd('/') + "/" + MakeFileName(pose) + ".png";
        }

        private static string MakeFileName(PoseClip pose)
        {
            var value = pose != null ? pose.displayName : "";
            var safe = string.IsNullOrWhiteSpace(value) ? "Pose" : value.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }

            var guid = pose != null ? PoseTuneNames.ShortGuid(pose.StableGuid) : "unknown";
            return safe + "_" + guid;
        }
    }
}
