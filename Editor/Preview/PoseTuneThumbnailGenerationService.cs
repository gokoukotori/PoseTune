using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneThumbnailGenerationService
    {
        private readonly PoseTuneIconCacheService iconCache = new();

        public Texture2D Generate(PoseClip pose, PoseTuneRoot root)
        {
            if (pose == null || root == null)
            {
                return null;
            }

            var graph = new PoseGraphCollector().Collect(root);
            if (!iconCache.TryGetIconsFolder(graph, out var folder))
            {
                PoseTuneLog.Error("thumbnail生成には保存済みSceneまたはPrefab上のPoseTuneRootが必要です。", root);
                return null;
            }

            return ThumbnailRenderer.GenerateThumbnail(pose, folder, root);
        }
    }
}
