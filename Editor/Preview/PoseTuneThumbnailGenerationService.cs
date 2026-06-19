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
            return ThumbnailRenderer.GenerateThumbnail(pose, iconCache.IconsFolder(graph), root);
        }
    }
}
