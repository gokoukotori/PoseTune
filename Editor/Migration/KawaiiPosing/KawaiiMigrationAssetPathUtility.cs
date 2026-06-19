using System.IO;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiMigrationAssetPathUtility
    {
        public static string MigrationRoot(GameObject avatarRoot)
        {
            var name = avatarRoot != null ? avatarRoot.name : "Avatar";
            return "Assets/PoseTuneGenerated/KawaiiMigration/" + Sanitize(name);
        }

        public static string ClipsPath(GameObject avatarRoot)
        {
            return Path.Combine(MigrationRoot(avatarRoot), "Clips").Replace('\\', '/');
        }

        public static string BlendTreesPath(GameObject avatarRoot)
        {
            return Path.Combine(MigrationRoot(avatarRoot), "BlendTrees").Replace('\\', '/');
        }

        public static string ReportsPath(GameObject avatarRoot)
        {
            return Path.Combine(MigrationRoot(avatarRoot), "Reports").Replace('\\', '/');
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "Avatar" : value;
        }
    }
}
