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

                    foreach (var pose in group.GetComponentsInChildren<PoseClip>(true).OrderBy(p => p.menuOrder))
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
            EditorGUILayout.Toggle("終了時にリセット", summary.GenerateResetOnExit);
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
            var policy = pose.GetComponents<PoseTrackingPolicy>().FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
            if (policy != null)
            {
                DrawPolicyObject(policy);
                return;
            }

            var serializedPose = new SerializedObject(pose);
            serializedPose.Update();
            EditorGUILayout.PropertyField(serializedPose.FindProperty("tracking"), true);
            serializedPose.ApplyModifiedProperties();
            if (GUILayout.Button("PoseTrackingPolicy component を追加"))
            {
                Undo.AddComponent<PoseTrackingPolicy>(pose.gameObject);
            }
        }

        private static void DrawRootPolicyEditor(PoseTuneRoot root)
        {
            var policy = PoseTuneTrackingPolicyResolver.RootPolicy(root);
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
            var policy = gameObject.GetComponents<PoseTrackingPolicy>().FirstOrDefault(PoseTuneAuthoringInclusion.ComponentEnabled);
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
            var serializedPolicy = new SerializedObject(policy);
            serializedPolicy.Update();
            EditorGUILayout.PropertyField(serializedPolicy.FindProperty("tracking"), true);
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
