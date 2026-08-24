using System;
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

    internal sealed class PoseTunePresetApplyPlan
    {
        internal readonly PoseTuneRoot Root;
        internal readonly PoseTunePreset Preset;
        internal readonly PoseTunePresetApplyMode Mode;
        internal readonly List<PoseTunePresetGroupOperation> GroupOperations = new();
        internal readonly List<PoseClip> PosesToRemove = new();
        internal readonly List<PoseGroup> GroupsToRemove = new();
        internal readonly List<Component> DependentComponentsToRemove = new();
        internal readonly List<string> Errors = new();

        internal PoseTunePresetApplyPlan(
            PoseTuneRoot root,
            PoseTunePreset preset,
            PoseTunePresetApplyMode mode)
        {
            Root = root;
            Preset = preset;
            Mode = mode;
        }

        internal bool IsValid => Errors.Count == 0;
        internal int AddedGroupCount => GroupOperations.Count(operation => operation.Existing == null);
        internal int UpdatedGroupCount => GroupOperations.Count(operation => operation.Existing != null);
        internal int RemovedGroupCount => GroupsToRemove.Count;
        internal int AddedPoseCount => GroupOperations.Sum(operation => operation.Poses.Count(pose => pose.Existing == null));
        internal int UpdatedPoseCount => GroupOperations.Sum(operation => operation.Poses.Count(pose => pose.Existing != null));
        internal int RemovedPoseCount => PosesToRemove.Count;
        internal int RemovedDependentComponentCount => DependentComponentsToRemove.Count;
    }

    internal sealed class PoseTunePresetGroupOperation
    {
        internal readonly PoseGroupPresetData Data;
        internal readonly PoseGroup Existing;
        internal readonly List<PoseTunePresetPoseOperation> Poses = new();

        internal PoseTunePresetGroupOperation(PoseGroupPresetData data, PoseGroup existing)
        {
            Data = data;
            Existing = existing;
        }
    }

    internal sealed class PoseTunePresetPoseOperation
    {
        internal readonly PoseClipPresetData Data;
        internal readonly PoseClip Existing;

        internal PoseTunePresetPoseOperation(PoseClipPresetData data, PoseClip existing)
        {
            Data = data;
            Existing = existing;
        }
    }

    public sealed class PoseTunePresetApplier
    {
        private const string UndoName = "PoseTune プリセットを適用";
        private const string PoseGroupsRootName = "ポーズグループ";

        public PoseTunePreset Capture(PoseTuneRoot root)
        {
            var preset = ScriptableObject.CreateInstance<PoseTunePreset>();
            preset.schemaVersion = PoseTunePreset.CurrentSchemaVersion;
            if (root == null)
            {
                return preset;
            }

            preset.presetName = root.displayName;
            preset.rootTrackingPolicy = PoseTunePresetMapper.CaptureTrackingPolicy(
                PoseTuneTrackingPolicyResolver.RootPolicy(root));
            var menu = OwnedComponents<PoseMenu>(root).FirstOrDefault();
            if (menu != null)
            {
                PoseTunePresetSettingsMapper.CaptureMenu(preset.menu, menu);
            }

            var height = OwnedComponents<PoseHeightAdjust>(root).FirstOrDefault();
            if (height != null)
            {
                PoseTunePresetSettingsMapper.CaptureHeight(preset.height, height);
            }

            preset.groups = OwnedComponents<PoseGroup>(root)
                .Where(PoseTuneAuthoringInclusion.Includes)
                .OrderBy(group => group.menuOrder)
                .ThenBy(group => group.displayName, StringComparer.Ordinal)
                .Select(group => PoseTunePresetMapper.CaptureGroup(root, group))
                .ToList();
            return preset;
        }

        public void Apply(PoseTuneRoot root, PoseTunePreset preset, PoseTunePresetApplyMode mode)
        {
            var plan = CreatePlan(root, preset, mode);
            if (!plan.IsValid)
            {
                foreach (var error in plan.Errors)
                {
                    Debug.LogError("PoseTune preset apply aborted: " + error, root);
                }

                return;
            }

            Commit(plan);
        }

        internal PoseTunePresetApplyPlan CreatePlan(
            PoseTuneRoot root,
            PoseTunePreset preset,
            PoseTunePresetApplyMode mode)
        {
            var plan = new PoseTunePresetApplyPlan(root, preset, mode);
            if (root == null)
            {
                plan.Errors.Add("PoseTuneRoot is null.");
                return plan;
            }

            if (preset == null)
            {
                plan.Errors.Add("PoseTunePreset is null.");
                return plan;
            }

            if (preset.schemaVersion != PoseTunePreset.CurrentSchemaVersion)
            {
                plan.Errors.Add(
                    $"Unsupported preset schema version {preset.schemaVersion}. " +
                    $"Only schema v{PoseTunePreset.CurrentSchemaVersion} is supported; automatic migration is not available.");
                return plan;
            }

            if (EditorUtility.IsPersistent(root) || PrefabUtility.IsPartOfImmutablePrefab(root))
            {
                plan.Errors.Add("The target root is not editable in the current Scene or Prefab Stage.");
                return plan;
            }

            if (preset.groups == null)
            {
                plan.Errors.Add("Preset groups is null. A malformed preset is not interpreted as an empty replacement.");
                return plan;
            }

            var currentGroups = OwnedComponents<PoseGroup>(root).ToList();
            var currentPoses = OwnedComponents<PoseClip>(root).ToList();
            ValidateStableGuidUniqueness(currentGroups.Cast<Object>(), "current group", plan.Errors);
            ValidateStableGuidUniqueness(currentPoses.Cast<Object>(), "current pose", plan.Errors);
            ValidatePresetData(preset, plan.Errors);
            if (!plan.IsValid)
            {
                return plan;
            }

            var usedGroups = new HashSet<PoseGroup>();
            var usedPoses = new HashSet<PoseClip>();
            for (var groupIndex = 0; groupIndex < preset.groups.Count; groupIndex++)
            {
                var groupData = preset.groups[groupIndex];
                if (groupData == null)
                {
                    continue;
                }

                var group = MatchGroup(
                    currentGroups,
                    usedGroups,
                    groupData);
                if (plan.Errors.Count > 0)
                {
                    continue;
                }

                if (group != null)
                {
                    usedGroups.Add(group);
                }

                var groupOperation = new PoseTunePresetGroupOperation(groupData, group);
                plan.GroupOperations.Add(groupOperation);
                var groupPoses = group != null
                    ? PoseGroupOwnership.OwnedClips(group)
                        .Where(pose => NearestRoot(pose) == root)
                        .ToList()
                    : new List<PoseClip>();
                var usedGroupPoses = new HashSet<PoseClip>();
                for (var poseIndex = 0; poseIndex < groupData.poses.Count; poseIndex++)
                {
                    var poseData = groupData.poses[poseIndex];
                    if (poseData == null)
                    {
                        continue;
                    }

                    var poseStableGuid = NormalizeGuid(poseData.poseStableGuid);
                    if (!string.IsNullOrEmpty(poseStableGuid))
                    {
                        var crossGroupMatch = currentPoses.FirstOrDefault(pose =>
                            ReadStableGuid(pose) == poseStableGuid &&
                            (group == null || !PoseGroupOwnership.IsOwnedBy(group, pose)));
                        if (crossGroupMatch != null)
                        {
                            plan.Errors.Add(
                                $"Pose stable GUID '{poseStableGuid}' belongs to another group. " +
                                "Cross-group preset moves must be resolved explicitly before apply.");
                            continue;
                        }
                    }

                    var pose = MatchPose(
                        groupPoses,
                        usedGroupPoses,
                        poseData);
                    if (plan.Errors.Count > 0)
                    {
                        continue;
                    }

                    if (pose != null)
                    {
                        usedGroupPoses.Add(pose);
                        usedPoses.Add(pose);
                    }

                    groupOperation.Poses.Add(new PoseTunePresetPoseOperation(poseData, pose));
                }
            }

            if (!plan.IsValid)
            {
                plan.GroupOperations.Clear();
                return plan;
            }

            if (mode == PoseTunePresetApplyMode.Replace)
            {
                plan.PosesToRemove.AddRange(currentPoses.Where(pose => !usedPoses.Contains(pose)));
                plan.GroupsToRemove.AddRange(currentGroups.Where(group => !usedGroups.Contains(group)));
                CollectOrphanedDependentComponents(plan);
            }

            return plan;
        }

        internal bool Commit(PoseTunePresetApplyPlan plan)
        {
            if (plan == null || !plan.IsValid || plan.Root == null || plan.Preset == null)
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                ApplyMenu(plan.Root, plan.Preset.menu);
                ApplyHeight(plan.Root, plan.Preset.height);
                ApplyRootTrackingPolicy(plan);

                foreach (var groupOperation in plan.GroupOperations)
                {
                    var group = groupOperation.Existing ?? CreatePoseGroup(plan.Root, groupOperation.Data);
                    Undo.RecordObject(group, UndoName);
                    PoseTunePresetMapper.ApplyGroupData(plan.Root, group, groupOperation.Data);
                    ApplyGroupTrackingPolicy(plan, group, groupOperation.Data);
                    AssertStableGuidApplied(group, groupOperation.Data.groupStableGuid, "group");
                    RecordPrefabModifications(group);

                    foreach (var poseOperation in groupOperation.Poses)
                    {
                        var pose = poseOperation.Existing ?? CreatePoseClip(group, poseOperation.Data);
                        Undo.RecordObject(pose, UndoName);
                        PoseTunePresetMapper.ApplyPoseData(plan.Root, pose, poseOperation.Data);
                        AssertStableGuidApplied(pose, poseOperation.Data.poseStableGuid, "pose");
                        RecordPrefabModifications(pose);
                    }
                }

                foreach (var dependent in plan.DependentComponentsToRemove.Where(component => component != null))
                {
                    Undo.DestroyObjectImmediate(dependent);
                }

                foreach (var pose in plan.PosesToRemove.Where(pose => pose != null))
                {
                    Undo.DestroyObjectImmediate(pose);
                }

                foreach (var group in plan.GroupsToRemove.Where(group => group != null))
                {
                    Undo.DestroyObjectImmediate(group);
                }

                Undo.CollapseUndoOperations(undoGroup);
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception, plan.Root);
                return false;
            }
        }

        private static void ApplyRootTrackingPolicy(PoseTunePresetApplyPlan plan)
        {
            ApplyTrackingPolicyComponents(
                plan.Root.gameObject,
                PoseTuneTrackingPolicyResolver.RootPolicies(plan.Root, true),
                plan.Preset.rootTrackingPolicy,
                plan.Mode);
        }

        private static void ApplyGroupTrackingPolicy(
            PoseTunePresetApplyPlan plan,
            PoseGroup group,
            PoseGroupPresetData data)
        {
            ApplyTrackingPolicyComponents(
                group.gameObject,
                group.GetComponents<PoseTrackingPolicy>(),
                data.trackingPolicy,
                plan.Mode);
        }

        private static void ApplyTrackingPolicyComponents(
            GameObject addTarget,
            IEnumerable<PoseTrackingPolicy> existingPolicies,
            PoseTrackingPolicyPresetData data,
            PoseTunePresetApplyMode mode)
        {
            var policies = (existingPolicies ?? Enumerable.Empty<PoseTrackingPolicy>())
                .Where(policy => policy != null)
                .ToList();
            var shouldApply = data != null && data.present;
            if (!shouldApply)
            {
                if (mode == PoseTunePresetApplyMode.Replace)
                {
                    foreach (var policy in policies)
                    {
                        Undo.DestroyObjectImmediate(policy);
                    }
                }

                return;
            }

            var primary = policies.FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (primary == null)
            {
                foreach (var disabledOnTarget in policies.Where(policy =>
                             policy.gameObject == addTarget &&
                             !PoseTuneAuthoringInclusion.ComponentEnabled(policy)))
                {
                    Undo.DestroyObjectImmediate(disabledOnTarget);
                }

                primary = Undo.AddComponent<PoseTrackingPolicy>(addTarget);
            }

            if (!PoseTunePresetMapper.TrackingPolicyMatches(primary, data))
            {
                Undo.RecordObject(primary, UndoName);
                PoseTunePresetMapper.ApplyTrackingPolicyData(primary, data);
                RecordPrefabModifications(primary);
            }

            if (mode != PoseTunePresetApplyMode.Replace)
            {
                return;
            }

            foreach (var duplicate in policies.Where(policy => policy != null && policy != primary))
            {
                Undo.DestroyObjectImmediate(duplicate);
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

            var menu = OwnedComponents<PoseMenu>(root).FirstOrDefault();
            if (menu == null)
            {
                var go = CreateChild(root.transform, "メニュー");
                menu = Undo.AddComponent<PoseMenu>(go);
            }
            else
            {
                Undo.RecordObject(menu, UndoName);
            }

            PoseTunePresetSettingsMapper.ApplyMenu(menu, data);
            ((Behaviour)menu).enabled = true;
            RecordPrefabModifications(menu);
        }

        private static void ApplyHeight(PoseTuneRoot root, PoseHeightPresetData data)
        {
            if (data == null)
            {
                return;
            }

            var height = OwnedComponents<PoseHeightAdjust>(root).FirstOrDefault();
            if (height == null)
            {
                var go = CreateChild(root.transform, "高さ調整");
                height = Undo.AddComponent<PoseHeightAdjust>(go);
            }
            else
            {
                Undo.RecordObject(height, UndoName);
            }

            PoseTunePresetSettingsMapper.ApplyHeight(height, data);
            ((Behaviour)height).enabled = true;
            RecordPrefabModifications(height);
        }

        private static PoseGroup CreatePoseGroup(PoseTuneRoot root, PoseGroupPresetData data)
        {
            var displayName = string.IsNullOrWhiteSpace(data.displayName)
                ? PoseTuneTemplateFactory.DefaultDisplayName(data.kind)
                : data.displayName.Trim();
            var go = CreateChild(FindPoseGroupsRoot(root), displayName);
            return Undo.AddComponent<PoseGroup>(go);
        }

        private static PoseClip CreatePoseClip(PoseGroup group, PoseClipPresetData data)
        {
            var name = !string.IsNullOrWhiteSpace(data.displayName)
                ? data.displayName.Trim()
                : data.clip != null
                    ? data.clip.name
                    : "新規ポーズ";
            var go = CreateChild(group.transform, name);
            return Undo.AddComponent<PoseClip>(go);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            return go;
        }

        private static Transform FindPoseGroupsRoot(PoseTuneRoot root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(transform =>
                           NearestRoot(transform) == root &&
                           transform.name == PoseGroupsRootName)
                   ?? root.transform;
        }

        private static PoseGroup MatchGroup(
            IReadOnlyList<PoseGroup> groups,
            ISet<PoseGroup> used,
            PoseGroupPresetData data)
        {
            var stableGuid = NormalizeGuid(data.groupStableGuid);
            return groups.FirstOrDefault(group =>
                !used.Contains(group) && ReadStableGuid(group) == stableGuid);
        }

        private static PoseClip MatchPose(
            IReadOnlyList<PoseClip> poses,
            ISet<PoseClip> used,
            PoseClipPresetData data)
        {
            var stableGuid = NormalizeGuid(data.poseStableGuid);
            return poses.FirstOrDefault(pose =>
                !used.Contains(pose) && ReadStableGuid(pose) == stableGuid);
        }

        private static void ValidatePresetData(PoseTunePreset preset, ICollection<string> errors)
        {
            if (preset.rootTrackingPolicy == null)
            {
                errors.Add("Preset rootTrackingPolicy is null.");
            }

            var groupGuids = new Dictionary<string, int>(StringComparer.Ordinal);
            var poseGuids = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var groupIndex = 0; groupIndex < preset.groups.Count; groupIndex++)
            {
                var group = preset.groups[groupIndex];
                if (group == null)
                {
                    errors.Add($"Preset group[{groupIndex}] is null.");
                    continue;
                }

                ValidateGuid(group.groupStableGuid, $"Preset group[{groupIndex}]", errors);
                AddUniquePresetGuid(groupGuids, NormalizeGuid(group.groupStableGuid), groupIndex, "group", errors);
                if (group.trackingPolicy == null)
                {
                    errors.Add($"Preset group[{groupIndex}].trackingPolicy is null.");
                }

                if (group.poses == null)
                {
                    errors.Add($"Preset group[{groupIndex}].poses is null.");
                    continue;
                }

                for (var poseIndex = 0; poseIndex < group.poses.Count; poseIndex++)
                {
                    var pose = group.poses[poseIndex];
                    if (pose == null)
                    {
                        errors.Add($"Preset group[{groupIndex}].pose[{poseIndex}] is null.");
                        continue;
                    }

                    var guid = NormalizeGuid(pose.poseStableGuid);
                    ValidateGuid(
                        pose.poseStableGuid,
                        $"Preset group[{groupIndex}].pose[{poseIndex}]",
                        errors);
                    if (string.IsNullOrEmpty(guid))
                    {
                        continue;
                    }

                    var location = $"group[{groupIndex}].pose[{poseIndex}]";
                    if (poseGuids.TryGetValue(guid, out var previous))
                    {
                        errors.Add($"Duplicate preset pose stable GUID '{guid}' at {previous} and {location}.");
                    }
                    else
                    {
                        poseGuids.Add(guid, location);
                    }
                }
            }
        }

        private static void CollectOrphanedDependentComponents(PoseTunePresetApplyPlan plan)
        {
            var removedPoses = new HashSet<PoseClip>(plan.PosesToRemove);
            var removedGroups = new HashSet<PoseGroup>(plan.GroupsToRemove);
            var candidateObjects = plan.PosesToRemove.Cast<Component>()
                .Concat(plan.GroupsToRemove)
                .Where(component => component != null)
                .Select(component => component.gameObject)
                .Distinct();
            foreach (var gameObject in candidateObjects)
            {
                var hasRemainingOwner = gameObject.GetComponents<PoseClip>()
                                            .Any(pose => !removedPoses.Contains(pose)) ||
                                        gameObject.GetComponents<PoseGroup>()
                                            .Any(group => !removedGroups.Contains(group));
                if (hasRemainingOwner)
                {
                    continue;
                }

                plan.DependentComponentsToRemove.AddRange(gameObject.GetComponents<PoseCondition>());
                plan.DependentComponentsToRemove.AddRange(gameObject.GetComponents<PoseTrackingPolicy>());
            }
        }

        private static void AddUniquePresetGuid(
            IDictionary<string, int> seen,
            string guid,
            int index,
            string kind,
            ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            if (seen.TryGetValue(guid, out var previous))
            {
                errors.Add($"Duplicate preset {kind} stable GUID '{guid}' at [{previous}] and [{index}].");
            }
            else
            {
                seen.Add(guid, index);
            }
        }

        private static void ValidateStableGuidUniqueness(
            IEnumerable<Object> components,
            string kind,
            ICollection<string> errors)
        {
            foreach (var component in components)
            {
                var raw = ReadRawStableGuid(component);
                if (!string.IsNullOrWhiteSpace(raw) && !Guid.TryParse(raw.Trim(), out _))
                {
                    errors.Add($"Invalid {kind} stable GUID '{raw.Trim()}'.");
                }
            }

            foreach (var duplicate in components
                         .Select(component => new { Component = component, Guid = ReadStableGuid(component) })
                         .Where(item => !string.IsNullOrEmpty(item.Guid))
                         .GroupBy(item => item.Guid, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                errors.Add($"Duplicate {kind} stable GUID '{duplicate.Key}' has {duplicate.Count()} candidates.");
            }
        }

        private static string ReadStableGuid(Object component)
        {
            if (component == null)
            {
                return "";
            }

            return NormalizeGuid(ReadRawStableGuid(component));
        }

        private static string ReadRawStableGuid(Object component)
        {
            if (component == null)
            {
                return "";
            }

            using var serialized = new SerializedObject(component);
            return serialized.FindProperty("stableGuid.value")?.stringValue ?? "";
        }

        private static string NormalizeGuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return Guid.TryParse(value.Trim(), out var guid)
                ? guid.ToString("N")
                : value.Trim();
        }

        private static void ValidateGuid(string value, string location, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{location} requires a non-empty stable GUID.");
                return;
            }

            if (!Guid.TryParse(value.Trim(), out _))
            {
                errors.Add($"{location} has an invalid stable GUID '{value.Trim()}'.");
            }
        }

        private static IEnumerable<T> OwnedComponents<T>(PoseTuneRoot root) where T : Component
        {
            return root != null
                ? root.GetComponentsInChildren<T>(true).Where(component => NearestRoot(component) == root)
                : Enumerable.Empty<T>();
        }

        private static PoseTuneRoot NearestRoot(Component component)
        {
            return component != null ? component.GetComponentInParent<PoseTuneRoot>(true) : null;
        }

        private static void RecordPrefabModifications(Object target)
        {
            if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static void AssertStableGuidApplied(Object component, string requestedGuid, string kind)
        {
            var normalized = NormalizeGuid(requestedGuid);
            if (!string.IsNullOrEmpty(normalized) && ReadStableGuid(component) != normalized)
            {
                throw new InvalidOperationException(
                    $"Preset {kind} stable GUID '{normalized}' could not be applied without a collision.");
            }
        }

    }
}
