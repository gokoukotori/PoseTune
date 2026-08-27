using System;
using System.IO;
using System.Linq;
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

        public static bool TryMigrationRoot(GameObject avatarRoot, string runId, out string path)
        {
            path = "";
            if (avatarRoot == null ||
                !PoseTuneObjectIdentity.TryGetPersistentHash(avatarRoot.transform, out var avatarId))
            {
                return false;
            }

            var safeAvatarName = SanitizeSegment(avatarRoot.name, "Avatar", 64);
            var safeRunId = SanitizeSegment(runId, "run", 64);
            path = $"Assets/PoseTuneGenerated/KawaiiMigration/{safeAvatarName}_{avatarId}/{safeRunId}";
            return true;
        }

        public static bool TryMotionsPath(GameObject avatarRoot, string runId, out string path)
        {
            path = "";
            if (!TryMigrationRoot(avatarRoot, runId, out var root))
            {
                return false;
            }

            path = Combine(root, "Motions");
            return true;
        }

        public static bool TryReportsPath(GameObject avatarRoot, string runId, out string path)
        {
            path = "";
            if (!TryMigrationRoot(avatarRoot, runId, out var root))
            {
                return false;
            }

            path = Combine(root, "Reports");
            return true;
        }

        public static bool TryMotionFileName(
            PoseClip pose,
            string displayName,
            bool animationClip,
            out string fileName)
        {
            fileName = "";
            if (!PoseTuneObjectIdentity.TryGetPersistentHash(pose, out var poseId))
            {
                return false;
            }

            var name = SanitizeSegment(displayName, "Pose", 48);
            fileName = $"{name}_{poseId}{(animationClip ? ".anim" : ".asset")}";
            return true;
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

        private static string Combine(string parent, string child)
        {
            return Path.Combine(parent, child).Replace('\\', '/');
        }
    }
}
