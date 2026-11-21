using System.Linq;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPosingMigrationMenuItems
    {
        [MenuItem("GameObject/Gokoukotori/PoseTune/KawaiiPosing から移行", false, 40)]
        private static void MigrateFromKawaiiPosing()
        {
            if (!PoseTuneTemplateFactory.TryResolveSelectedAvatarOrWarn(out var avatar))
            {
                return;
            }

            KawaiiPosingMigrationWindow.Open(avatar.gameObject);
        }

        [MenuItem("GameObject/Gokoukotori/PoseTune/KawaiiPosing から移行", true)]
        private static bool ValidateMigrateFromKawaiiPosing()
        {
            var avatar = PoseTuneHierarchyUtility.ResolveAvatar(Selection.activeGameObject);
            return avatar != null && KawaiiPosingDetector.FindSystems(avatar.gameObject).Any();
        }
    }
}
