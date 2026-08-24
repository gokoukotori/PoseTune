using System.IO;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum PoseTuneThumbnailCacheStatus
    {
        Loaded,
        Missing,
        Invalid
    }

    internal readonly struct PoseTuneThumbnailCacheProbe
    {
        public PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus status, Texture2D texture)
        {
            Status = status;
            Texture = texture;
        }

        public PoseTuneThumbnailCacheStatus Status { get; }
        public Texture2D Texture { get; }
    }

    public sealed class PoseTuneIconCacheService
    {
        public string IconsFolder(PoseGraph graph)
        {
            return PoseTuneProjectAssetPaths.BakeRootPath(graph) + "/Icons";
        }

        public Texture2D LoadCachedThumbnail(PoseGraph graph, PoseDefinition pose)
        {
            return ProbeCachedThumbnail(graph, pose).Texture;
        }

        internal PoseTuneThumbnailCacheProbe ProbeCachedThumbnail(PoseGraph graph, PoseDefinition pose)
        {
            if (pose?.Source == null)
            {
                return new PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus.Missing, null);
            }

            var path = ThumbnailAssetPath(pose.Source, IconsFolder(graph));
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                return new PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus.Loaded, texture);
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
            {
                return new PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus.Invalid, null);
            }

            return new PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus.Missing, null);
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
