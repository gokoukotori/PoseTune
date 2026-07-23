using System;
using System.IO;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiMigrationAssetPathUtility
    {
        private static readonly string[] ReservedFileNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string MigrationRoot(GameObject avatarRoot, PoseTuneRoot root)
        {
            return MigrationRoot(
                avatarRoot != null ? avatarRoot.name : "Avatar",
                root != null ? root.StableGuid : "unknown");
        }

        public static string MigrationRoot(string avatarName, string rootStableGuid)
        {
            var safeAvatarName = SanitizeSegment(avatarName, "Avatar", 64);
            var rootGuid = NormalizeGuid(rootStableGuid);
            return $"Assets/PoseTuneGenerated/KawaiiMigration/{safeAvatarName}/{rootGuid}";
        }

        public static string MotionsPath(GameObject avatarRoot, PoseTuneRoot root)
        {
            return Combine(MigrationRoot(avatarRoot, root), "Motions");
        }

        public static string ReportsPath(GameObject avatarRoot, PoseTuneRoot root)
        {
            return Combine(MigrationRoot(avatarRoot, root), "Reports");
        }

        public static string MotionFileName(string displayName, string poseStableGuid, bool animationClip)
        {
            var name = SanitizeSegment(displayName, "Pose", 48);
            var guid = NormalizeGuid(poseStableGuid);
            return $"{name}_{guid}{(animationClip ? ".anim" : ".asset")}";
        }

        public static string SanitizeSegment(string value, string fallback, int maxLength)
        {
            value = value?.Trim() ?? "";
            foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }).Distinct())
            {
                value = value.Replace(c, '_');
            }

            value = value.Trim().TrimEnd('.', ' ');
            if (string.IsNullOrWhiteSpace(value) || ReservedFileNames.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                value = fallback;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength).TrimEnd('.', ' ');
        }

        private static string NormalizeGuid(string value)
        {
            if (Guid.TryParse(value, out var guid))
            {
                return guid.ToString("N");
            }

            var normalized = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            return SanitizeSegment(normalized, "unknown", 64);
        }

        private static string Combine(string parent, string child)
        {
            return Path.Combine(parent, child).Replace('\\', '/');
        }
    }
}
