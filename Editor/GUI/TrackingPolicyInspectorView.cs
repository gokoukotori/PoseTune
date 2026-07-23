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

        public static TrackingPolicySummary ResolveEffectivePolicy(PoseTuneRoot root, Object selectedObject)
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

        private static void DrawSummary(TrackingPolicySummary summary)
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
                    DrawPoseEditor(pose);
                    break;
                case PoseGroup group:
                    DrawPolicyComponentEditor(group.gameObject, "Group policy を追加");
                    break;
                default:
                    DrawRootPolicyEditor(root);
                    break;
            }
        }

        private static void DrawPoseEditor(PoseClip pose)
        {
            var policy = pose.GetComponents<PoseTrackingPolicy>().FirstOrDefault();
            if (policy != null)
            {
                DrawPolicyObject(policy);
                return;
            }

            if (TrackingPolicyUtility.WasCustomizedFromPoseDefault(pose.tracking))
            {
                EditorGUILayout.HelpBox(
                    "この PoseClip には旧形式の inline tracking 値があります。Build 互換のため読み取られますが、新規編集は PoseTrackingPolicy に変換してください。",
                    MessageType.Warning);
                if (GUILayout.Button("旧 tracking 値を PoseTrackingPolicy へ変換"))
                {
                    ConvertLegacyInlinePolicy(pose);
                }

                return;
            }

            if (GUILayout.Button("PoseTrackingPolicy component を追加"))
            {
                Undo.AddComponent<PoseTrackingPolicy>(pose.gameObject);
            }
        }

        private static void DrawRootPolicyEditor(PoseTuneRoot root)
        {
            var policy = PoseTuneTrackingPolicyResolver.RootPolicy(root) ??
                         root.GetComponentsInChildren<PoseTrackingPolicy>(true)
                             .FirstOrDefault(candidate => IsRootLevelPolicy(root, candidate));
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

        private static void DrawPolicyComponentEditor(GameObject gameObject, string addLabel)
        {
            var policy = gameObject.GetComponents<PoseTrackingPolicy>().FirstOrDefault();
            if (policy != null)
            {
                DrawPolicyObject(policy);
                return;
            }

            if (GUILayout.Button(addLabel))
            {
                Undo.AddComponent<PoseTrackingPolicy>(gameObject);
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

        private static void ConvertLegacyInlinePolicy(PoseClip pose)
        {
            if (pose == null || pose.GetComponent<PoseTrackingPolicy>() != null)
            {
                return;
            }

            var legacyTracking = TrackingPolicyUtility.Copy(pose.tracking);
            Undo.RecordObject(pose, "旧 tracking 値を PoseTrackingPolicy へ変換");
            var policy = Undo.AddComponent<PoseTrackingPolicy>(pose.gameObject);
            Undo.RecordObject(policy, "旧 tracking 値を PoseTrackingPolicy へ変換");
            policy.tracking = legacyTracking;
            policy.useFullBodyTrackingOverride = false;
            policy.generateResetOnExit = true;
            pose.tracking = TrackingPolicyData.DefaultForPose();
            EditorUtility.SetDirty(pose);
            EditorUtility.SetDirty(policy);
        }

        private static bool IsValidSelection(PoseTuneRoot root, Object selectedObject)
        {
            if (selectedObject == null || selectedObject == root)
            {
                return true;
            }

            return selectedObject is Component component && component.GetComponentInParent<PoseTuneRoot>(true) == root;
        }

        private static bool IsRootLevelPolicy(PoseTuneRoot root, PoseTrackingPolicy policy)
        {
            if (root == null || policy == null)
            {
                return false;
            }

            return policy.transform == root.transform ||
                   (policy.transform.parent == root.transform &&
                    policy.GetComponent<PoseGroup>() == null &&
                    policy.GetComponent<PoseClip>() == null);
        }

    }
}
