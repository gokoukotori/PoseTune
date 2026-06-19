using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPosingMigrationMenuItems
    {
        [MenuItem("GameObject/PoseTune/KawaiiPosing から移行", false, 40)]
        private static void MigrateFromKawaiiPosing()
        {
            var avatar = ResolveAvatar(Selection.activeGameObject);
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("PoseTune", "VRCAvatarDescriptor を持つ Avatar を選択してください。", "OK");
                return;
            }

            KawaiiPosingMigrationWindow.Open(avatar.gameObject);
        }

        [MenuItem("GameObject/PoseTune/KawaiiPosing から移行", true)]
        private static bool ValidateMigrateFromKawaiiPosing()
        {
            var avatar = ResolveAvatar(Selection.activeGameObject);
            return avatar != null && KawaiiPosingDetector.FindSystems(avatar.gameObject).Any();
        }

        private static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            return selected != null
                ? selected.GetComponent<VRCAvatarDescriptor>()
                  ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                  ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true)
                : null;
        }
    }
}
