using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneNdmfAssetSaver
    {
        public static void SaveGeneratedAnimatorAssets(BuildContext context, AnimatorBuildResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!TrySaveGeneratedAnimatorAssets(context, result))
            {
                throw new InvalidOperationException("NDMF AssetSaver が利用できません。");
            }
        }

        public static bool TrySaveGeneratedAnimatorAssets(BuildContext context, AnimatorBuildResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return TrySaveGeneratedAnimatorAssets(context.AssetSaver, result);
        }

        public static bool TrySaveGeneratedAnimatorAssets(IAssetSaver assetSaver, AnimatorBuildResult result)
        {
            if (assetSaver == null || result == null)
            {
                return false;
            }

            var assets = EnumerateSaveTargets(result);
            if (assets.Length == 0)
            {
                return false;
            }

            assetSaver.SaveAssets(assets);
            var persisted = new HashSet<Object>(
                assetSaver.GetPersistedAssets().Where(asset => asset != null),
                ReferenceEqualityComparer<Object>.Instance);
            return assets.All(asset => EditorUtility.IsPersistent(asset) || persisted.Contains(asset));
        }

        public static void SaveGeneratedAnimatorAssets(IAssetSaver assetSaver, AnimatorBuildResult result)
        {
            if (assetSaver == null)
            {
                throw new ArgumentNullException(nameof(assetSaver));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!TrySaveGeneratedAnimatorAssets(assetSaver, result))
            {
                throw new InvalidOperationException("NDMF AssetSaver が PoseTune の生成 animator assets を保存しませんでした。");
            }
        }

        private static Object[] EnumerateSaveTargets(AnimatorBuildResult result)
        {
            return result.EnumerateGeneratedAssets()
                .Where(asset => asset != null)
                .Distinct(ReferenceEqualityComparer<Object>.Instance)
                .ToArray();
        }
    }
}
