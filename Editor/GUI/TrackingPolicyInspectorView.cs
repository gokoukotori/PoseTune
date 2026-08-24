using System;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class TrackingPolicyInspectorView
    {
        public void Draw(PoseTuneRoot root, ref Object selectedObject)
        {
            if (root == null)
            {
                return;
            }

            selectedObject = EditorGUILayout.ObjectField("対象", selectedObject, typeof(Object), true);
            if (!IsValidSelection(root, selectedObject))
            {
                selectedObject = root;
            }

            DrawSelectionButtons(root, ref selectedObject);
            var summary = ResolveEffectivePolicy(root, selectedObject);
            DrawSummary(summary);
            DrawDirectEditor(root, selectedObject);
        }

        public static ResolvedGroupTrackingPolicy ResolveEffectivePolicy(PoseTuneRoot root, Object selectedObject)
        {
            return PoseTuneTrackingPolicyResolver.ResolveEffectivePolicy(root, selectedObject);
        }

        private static void DrawSelectionButtons(PoseTuneRoot root, ref Object selectedObject)
        {
            EditorGUILayout.LabelField("PoseClip", EditorStyles.boldLabel);
            foreach (var group in root.GetComponentsInChildren<PoseGroup>(true).OrderBy(g => g.menuOrder))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(group.displayName, GUILayout.Width(140)))
                    {
                        selectedObject = group;
                    }

                    foreach (var pose in PoseGroupOwnership.OwnedClips(group).OrderBy(p => p.menuOrder))
                    {
                        if (GUILayout.Button(pose.displayName))
                        {
                            selectedObject = pose;
                        }
                    }
                }
            }
        }

        private static void DrawSummary(ResolvedGroupTrackingPolicy summary)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Effective policy", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Source", summary.Source.ToString());
            EditorGUILayout.LabelField("Context", summary.Context != null ? summary.Context.name : "<none>");
            EditorGUILayout.LabelField("Head / Hands / Hip / Feet",
                $"{summary.Policy.head} / {summary.Policy.leftHand},{summary.Policy.rightHand} / {summary.Policy.hip} / {summary.Policy.leftFoot},{summary.Policy.rightFoot}");
            EditorGUILayout.LabelField("Fingers / Eyes / Mouth",
                $"{summary.Policy.leftFingers},{summary.Policy.rightFingers} / {summary.Policy.eyes} / {summary.Policy.mouth}");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("終了時にリセット", summary.GenerateResetOnExit);
                EditorGUILayout.Toggle("FBT override", summary.HasFullBodyTrackingOverride);
            }

            if (summary.HasFullBodyTrackingOverride)
            {
                EditorGUILayout.LabelField("FBT: Head / Hands / Hip / Feet",
                    $"{summary.FullBodyTrackingPolicy.head} / {summary.FullBodyTrackingPolicy.leftHand},{summary.FullBodyTrackingPolicy.rightHand} / {summary.FullBodyTrackingPolicy.hip} / {summary.FullBodyTrackingPolicy.leftFoot},{summary.FullBodyTrackingPolicy.rightFoot}");
                EditorGUILayout.LabelField("FBT: Fingers / Eyes / Mouth",
                    $"{summary.FullBodyTrackingPolicy.leftFingers},{summary.FullBodyTrackingPolicy.rightFingers} / {summary.FullBodyTrackingPolicy.eyes} / {summary.FullBodyTrackingPolicy.mouth}");
            }
        }

        private static void DrawDirectEditor(PoseTuneRoot root, Object selectedObject)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("直接編集", EditorStyles.boldLabel);
            switch (selectedObject)
            {
                case PoseClip pose:
                    DrawPoseEditor(root, pose);
                    break;
                case PoseGroup group:
                    DrawGroupPolicyEditor(root, group);
                    break;
                default:
                    DrawRootPolicyEditor(root);
                    break;
            }
        }

        private static void DrawPoseEditor(PoseTuneRoot root, PoseClip pose)
        {
            var group = pose != null ? pose.GetComponentInParent<PoseGroup>(true) : null;
            if (group == null)
            {
                EditorGUILayout.HelpBox("所属する PoseGroup が見つかりません。", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "Tracking policy は Pose 単位では編集できません。所属 Group の全 Pose に共通で適用されます。",
                MessageType.Info);
            if (GUILayout.Button($"Group '{group.displayName}' を編集"))
            {
                Selection.activeObject = group;
            }

            DrawGroupPolicyEditor(root, group);
        }

        private static void DrawRootPolicyEditor(PoseTuneRoot root)
        {
            var policy = PoseTuneTrackingPolicyResolver.RootPolicy(root) ??
                         PoseTuneTrackingPolicyResolver.RootPolicies(root, true).FirstOrDefault();
            if (policy != null)
            {
                DrawPolicyObject(policy);
                return;
            }

            if (GUILayout.Button("Root fallback policy を追加"))
            {
                Undo.AddComponent<PoseTrackingPolicy>(root.gameObject);
            }
        }

        private static void DrawGroupPolicyEditor(PoseTuneRoot root, PoseGroup group)
        {
            var policy = PoseTuneTrackingPolicyResolver.GroupPolicy(group);
            if (policy != null)
            {
                DrawPolicyObject(policy);
                return;
            }

            if (group.GetComponents<PoseTrackingPolicy>().Any())
            {
                EditorGUILayout.HelpBox(
                    "無効な Group policy は effective policy では存在しないものとして扱います。追加すると新しい有効な component へ置き換えます。",
                    MessageType.Info);
            }

            if (GUILayout.Button("Group policy を追加"))
            {
                AddGroupPolicyPreservingEffective(root, group);
            }
        }

        internal static PoseTrackingPolicy AddGroupPolicyPreservingEffective(PoseTuneRoot root, PoseGroup group)
        {
            if (root == null || group == null || group.GetComponentInParent<PoseTuneRoot>(true) != root)
            {
                return null;
            }

            var existing = PoseTuneTrackingPolicyResolver.GroupPolicy(group);
            if (existing != null)
            {
                return existing;
            }

            if (EditorUtility.IsPersistent(group) || PrefabUtility.IsPartOfImmutablePrefab(group))
            {
                return null;
            }

            var effective = PoseTuneTrackingPolicyResolver.ResolveGroupPolicy(root, group);
            const string undoName = "Group policy を追加";
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            try
            {
                foreach (var disabled in group.GetComponents<PoseTrackingPolicy>()
                             .Where(policy => policy != null && !PoseTuneAuthoringInclusion.ComponentEnabled(policy)))
                {
                    Undo.DestroyObjectImmediate(disabled);
                }

                var policy = Undo.AddComponent<PoseTrackingPolicy>(group.gameObject);
                Undo.RecordObject(policy, undoName);
                TrackingPolicyUtility.ApplyResolved(policy, effective);
                EditorUtility.SetDirty(policy);
                if (PrefabUtility.IsPartOfPrefabInstance(policy))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(policy);
                }

                Undo.CollapseUndoOperations(undoGroup);
                return policy;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception, group);
                return null;
            }
        }

        private static void DrawPolicyObject(PoseTrackingPolicy policy)
        {
            if (!policy.enabled)
            {
                EditorGUILayout.HelpBox("この policy component は無効なため、effective policy の解決には使用されません。", MessageType.Info);
                if (GUILayout.Button("Policy component を有効化"))
                {
                    Undo.RecordObject(policy, "PoseTrackingPolicy を有効化");
                    policy.enabled = true;
                    EditorUtility.SetDirty(policy);
                }
            }

            var serializedPolicy = new SerializedObject(policy);
            serializedPolicy.Update();
            EditorGUILayout.PropertyField(serializedPolicy.FindProperty("tracking"), true);
            EditorGUILayout.PropertyField(serializedPolicy.FindProperty("useFullBodyTrackingOverride"));
            if (serializedPolicy.FindProperty("useFullBodyTrackingOverride").boolValue)
            {
                EditorGUILayout.PropertyField(serializedPolicy.FindProperty("fullBodyTracking"), true);
            }
            EditorGUILayout.PropertyField(serializedPolicy.FindProperty("generateResetOnExit"));
            serializedPolicy.ApplyModifiedProperties();
        }

        private static bool IsValidSelection(PoseTuneRoot root, Object selectedObject)
        {
            if (selectedObject == null || selectedObject == root)
            {
                return true;
            }

            return selectedObject is Component component && component.GetComponentInParent<PoseTuneRoot>(true) == root;
        }

    }
}
