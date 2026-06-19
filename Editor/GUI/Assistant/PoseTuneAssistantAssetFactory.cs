using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantAssetFactory
    {
        public static T Create<T>(PoseTuneRoot root, string name) where T : ScriptableObject
        {
            var graph = new PoseGraphCollector().Collect(root);
            var folder = PoseTuneProjectAssetPaths.BakeRootPath(graph) + "/Presets";
            PoseTuneProjectAssetUtility.EnsureFolder(folder);
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
