using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAdjustmentPresetApplier
    {
        public static AvatarAdjustmentPreset Capture(PoseTuneRoot root, GameObject avatarRoot)
        {
            var preset = ScriptableObject.CreateInstance<AvatarAdjustmentPreset>();
            preset.avatarName = avatarRoot != null ? avatarRoot.name : "";
            preset.avatarAssetGuidHash = AvatarGuidHash(avatarRoot);
            if (root == null)
            {
                return preset;
            }

            preset.adjustments = root.GetComponentsInChildren<PoseClip>(true)
                .Select(pose => new PoseAdjustmentEntry
                {
                    poseStableGuid = pose.StableGuid,
                    adjustmentClip = pose.adjustmentClip,
                    rootOffset = pose.rootOffset,
                    cameraOffset = pose.cameraOffset
                })
                .ToList();
            return preset;
        }

        public static ValidationReport Apply(PoseTuneRoot root, AvatarAdjustmentPreset preset)
        {
            var report = new ValidationReport();
            if (root == null || preset == null)
            {
                return report;
            }

            var avatar = root.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
            var avatarHash = AvatarGuidHash(avatar != null ? avatar.gameObject : null);
            if (!string.IsNullOrWhiteSpace(preset.avatarAssetGuidHash) &&
                preset.avatarAssetGuidHash != avatarHash)
            {
                report.Warning(PoseTuneDiagnostics.AdjustmentPresetAvatarHashMismatch.Code, "avatar 調整 preset の対象 avatar が異なる可能性があります。", preset);
            }

            var duplicatePoseGuids = root.GetComponentsInChildren<PoseClip>(true)
                .Where(pose => !string.IsNullOrWhiteSpace(pose.StableGuid))
                .GroupBy(pose => pose.StableGuid)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();
            foreach (var duplicate in duplicatePoseGuids)
            {
                report.Warning(PoseTuneDiagnostics.AdjustmentPresetDuplicatePoseStableGuid.Code, "PoseClip の stable GUID が重複しているため調整 preset を安全に適用できません: " + duplicate, root);
            }

            var poses = root.GetComponentsInChildren<PoseClip>(true)
                .Where(pose => !string.IsNullOrWhiteSpace(pose.StableGuid) && !duplicatePoseGuids.Contains(pose.StableGuid))
                .ToDictionary(pose => pose.StableGuid);
            foreach (var entry in preset.adjustments ?? Enumerable.Empty<PoseAdjustmentEntry>())
            {
                if (!string.IsNullOrWhiteSpace(entry.poseStableGuid) && duplicatePoseGuids.Contains(entry.poseStableGuid))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.poseStableGuid) ||
                    !poses.TryGetValue(entry.poseStableGuid, out var pose))
                {
                    report.Warning(PoseTuneDiagnostics.AdjustmentPresetPoseStableGuidMissing.Code, "調整 preset の poseStableGuid に一致する PoseClip がありません: " + entry.poseStableGuid, preset);
                    continue;
                }

                pose.adjustmentClip = entry.adjustmentClip;
                pose.rootOffset = entry.rootOffset;
                pose.cameraOffset = entry.cameraOffset;
                EditorUtility.SetDirty(pose);
            }

            return report;
        }

        private static string AvatarGuidHash(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                return "";
            }

            var path = AssetDatabase.GetAssetPath(avatarRoot);
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return "";
            }

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(guid));
            return string.Concat(bytes.Select(value => value.ToString("x2")));
        }
    }
}
