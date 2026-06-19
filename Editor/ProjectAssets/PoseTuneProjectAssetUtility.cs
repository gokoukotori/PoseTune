using System;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneProjectAssetUtility
    {
        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new ArgumentException($"PoseTune の生成 asset path は Assets 配下である必要があります: {assetPath}");
            }

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        public static void ClearGeneratedAssetFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var prefix = assetPath.TrimEnd('/') + "/";
            var iconFolder = prefix + "Icons";
            var iconPrefix = iconFolder + "/";
            foreach (var guid in AssetDatabase.FindAssets("", new[] { assetPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path) || path == assetPath || !path.StartsWith(prefix))
                {
                    continue;
                }

                if (path == iconFolder || path.StartsWith(iconPrefix))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
