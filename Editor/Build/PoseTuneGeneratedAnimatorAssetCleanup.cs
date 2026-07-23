using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneGeneratedAnimatorAssetCleanup
    {
        public static void DestroyUnsaved(AnimatorBuildResult result)
        {
            if (result == null)
            {
                return;
            }

            var assets = result.GeneratedAssets
                .Cast<Object>()
                .Concat(new Object[] { result.TargetController, result.FxController })
                .Where(asset => asset != null && !EditorUtility.IsPersistent(asset))
                .Distinct(ReferenceEqualityComparer<Object>.Instance)
                .Reverse()
                .ToArray();

            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }

            result.GeneratedAssets.Clear();
            result.TargetController = null;
            result.FxController = null;
        }
    }
}
