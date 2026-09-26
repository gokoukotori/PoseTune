using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPresetAdjustmentSyncMenuItems
    {
        private const string MenuPath = "GameObject/Gokoukotori/PoseTune/KawaiiPosing 調整クリップを同期";

        [MenuItem(MenuPath, false, 41)]
        private static void OpenWindow()
        {
            var roots = ResolveRoots(Selection.activeGameObject);
            KawaiiPresetAdjustmentSyncWindow.Open(roots.Length == 1 ? roots[0] : null);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateOpenWindow()
        {
            return Selection.activeGameObject != null;
        }

        private static PoseTuneRoot[] ResolveRoots(GameObject selected)
        {
            if (selected == null)
            {
                return System.Array.Empty<PoseTuneRoot>();
            }

            var nearest = selected.GetComponent<PoseTuneRoot>() ?? selected.GetComponentInParent<PoseTuneRoot>(true);
            if (nearest != null)
            {
                return new[] { nearest };
            }

            return selected.GetComponentsInChildren<PoseTuneRoot>(true)
                .Where(root => root != null)
                .Distinct()
                .ToArray();
        }
    }
}
