using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public enum PoseTunePresetApplyMode
    {
        Merge,
        Replace
    }

    public sealed class PoseTunePresetApplier
    {
        public PoseTunePreset Capture(PoseTuneRoot root)
        {
            var preset = ScriptableObject.CreateInstance<PoseTunePreset>();
            if (root == null)
            {
                return preset;
            }

            preset.presetName = root.displayName;
            var menu = root.GetComponentInChildren<PoseMenu>(true);
            if (menu != null)
            {
                PoseTunePresetSettingsMapper.CaptureMenu(preset.menu, menu);
            }

            var height = root.GetComponentInChildren<PoseHeightAdjust>(true);
            if (height != null)
            {
                PoseTunePresetSettingsMapper.CaptureHeight(preset.height, height);
            }

            preset.groups = root.GetComponentsInChildren<PoseGroup>(true)
                .Where(PoseTuneAuthoringInclusion.Includes)
                .OrderBy(group => group.menuOrder)
                .ThenBy(group => group.displayName)
                .Select(PoseTunePresetMapper.CaptureGroup)
                .ToList();
            return preset;
        }

        public void Apply(PoseTuneRoot root, PoseTunePreset preset, PoseTunePresetApplyMode mode)
        {
            if (root == null || preset == null)
            {
                return;
            }

            ApplyMenu(root, preset.menu);
            ApplyHeight(root, preset.height);

            var usedGroups = new HashSet<PoseGroup>();
            foreach (var groupData in preset.groups ?? Enumerable.Empty<PoseGroupPresetData>())
            {
                var group = FindMatchingGroup(root, groupData, usedGroups) ?? PoseTuneAuthoringFactory.AddPoseGroup(root, groupData.kind);
                usedGroups.Add(group);
                PoseTunePresetMapper.ApplyGroupData(root, group, groupData);
                if (mode == PoseTunePresetApplyMode.Replace)
                {
                    foreach (var pose in group.GetComponentsInChildren<PoseClip>(true).ToArray())
                    {
                        Object.DestroyImmediate(pose.gameObject);
                    }
                }

                ApplyPoses(root, group, groupData, mode);
                EditorUtility.SetDirty(group);
            }
        }

        public AvatarAdjustmentPreset CaptureAdjustments(PoseTuneRoot root, GameObject avatarRoot)
        {
            return PoseTuneAdjustmentPresetApplier.Capture(root, avatarRoot);
        }

        public ValidationReport ApplyAdjustments(PoseTuneRoot root, AvatarAdjustmentPreset preset)
        {
            return PoseTuneAdjustmentPresetApplier.Apply(root, preset);
        }

        private static void ApplyMenu(PoseTuneRoot root, PoseMenuPresetData data)
        {
            if (data == null)
            {
                return;
            }

            var menu = root.GetComponentInChildren<PoseMenu>(true);
            if (menu == null)
            {
                var go = new GameObject("メニュー");
                Undo.RegisterCreatedObjectUndo(go, "PoseTune メニューを追加");
                go.transform.SetParent(root.transform, false);
                menu = go.AddComponent<PoseMenu>();
            }

            PoseTunePresetSettingsMapper.ApplyMenu(menu, data);
            EditorUtility.SetDirty(menu);
        }

        private static void ApplyHeight(PoseTuneRoot root, PoseHeightPresetData data)
        {
            if (data == null)
            {
                return;
            }

            var height = root.GetComponentInChildren<PoseHeightAdjust>(true);
            if (height == null)
            {
                var go = new GameObject("高さ調整");
                Undo.RegisterCreatedObjectUndo(go, "PoseTune 高さ調整を追加");
                go.transform.SetParent(root.transform, false);
                height = go.AddComponent<PoseHeightAdjust>();
            }

            PoseTunePresetSettingsMapper.ApplyHeight(height, data);
            EditorUtility.SetDirty(height);
        }

        private static PoseGroup FindMatchingGroup(
            PoseTuneRoot root,
            PoseGroupPresetData data,
            HashSet<PoseGroup> usedGroups)
        {
            var groups = root.GetComponentsInChildren<PoseGroup>(true)
                .Where(group => !usedGroups.Contains(group))
                .ToList();
            return groups.FirstOrDefault(group => !string.IsNullOrWhiteSpace(data.groupStableGuid) &&
                                                  group.StableGuid == data.groupStableGuid)
                   ?? groups.FirstOrDefault(group => group.kind == data.kind &&
                                                  !string.IsNullOrWhiteSpace(group.parameterName) &&
                                                  group.parameterName == data.parameterName)
                   ?? groups.FirstOrDefault(group => group.kind == data.kind && group.displayName == data.displayName)
                   ?? groups.FirstOrDefault(group => group.kind == data.kind);
        }

        private static void ApplyPoses(PoseTuneRoot root, PoseGroup group, PoseGroupPresetData groupData, PoseTunePresetApplyMode mode)
        {
            var usedPoses = new HashSet<PoseClip>();
            foreach (var poseData in groupData.poses ?? Enumerable.Empty<PoseClipPresetData>())
            {
                var pose = mode == PoseTunePresetApplyMode.Merge
                    ? FindMatchingPose(group, poseData, usedPoses)
                    : null;
                pose ??= PoseTuneAuthoringFactory.AddPoseClip(group, poseData.clip);
                usedPoses.Add(pose);
                PoseTunePresetMapper.ApplyPoseData(root, pose, poseData);
                EditorUtility.SetDirty(pose);
            }
        }

        private static PoseClip FindMatchingPose(PoseGroup group, PoseClipPresetData data, HashSet<PoseClip> usedPoses)
        {
            var poses = group.GetComponentsInChildren<PoseClip>(true)
                .Where(pose => !usedPoses.Contains(pose))
                .ToList();
            return poses.FirstOrDefault(pose => ClipMatches(pose.clip, data.clip))
                   ?? poses.FirstOrDefault(pose => pose.displayName == data.displayName)
                   ?? poses.FirstOrDefault(pose => !string.IsNullOrWhiteSpace(data.poseStableGuid) &&
                                                   pose.StableGuid == data.poseStableGuid);
        }

        private static bool ClipMatches(AnimationClip left, AnimationClip right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (left == right)
            {
                return true;
            }

            var leftPath = AssetDatabase.GetAssetPath(left);
            var rightPath = AssetDatabase.GetAssetPath(right);
            return !string.IsNullOrWhiteSpace(leftPath) && leftPath == rightPath;
        }

    }
}
