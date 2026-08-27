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
        Invalid,
        IdentityUnavailable
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
        public bool TryGetIconsFolder(PoseGraph graph, out string folder)
        {
            if (!PoseTuneProjectAssetPaths.TryGetBakeRootPath(graph, out var rootPath))
            {
                folder = "";
                return false;
            }

            folder = rootPath + "/Icons";
            return true;
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

            if (!TryGetIconsFolder(graph, out var folder) ||
                !TryGetThumbnailAssetPath(pose.Source, folder, out var path))
            {
                return new PoseTuneThumbnailCacheProbe(PoseTuneThumbnailCacheStatus.IdentityUnavailable, null);
            }

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

        public bool TryGetThumbnailAssetPath(PoseClip pose, string folder, out string path)
        {
            path = "";
            if (string.IsNullOrWhiteSpace(folder) || !TryMakeFileName(pose, out var fileName))
            {
                return false;
            }

            path = folder.TrimEnd('/') + "/" + fileName + ".png";
            return true;
        }

        private static bool TryMakeFileName(PoseClip pose, out string fileName)
        {
            fileName = "";
            if (pose == null || !PoseTuneObjectIdentity.TryGetPersistentHash(pose, out var objectHash))
            {
                return false;
            }

            var value = pose != null ? pose.displayName : "";
            var safe = string.IsNullOrWhiteSpace(value) ? "Pose" : value.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }

            fileName = safe + "_" + objectHash;
            return true;
        }
    }
}
