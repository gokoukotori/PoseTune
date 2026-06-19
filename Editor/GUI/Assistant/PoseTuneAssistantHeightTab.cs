using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantHeightTab
    {
        private static readonly Dictionary<int, PoseClip> GroundingPoseSelections = new();
        private static readonly Dictionary<int, PoseGroundingSuggestion> GroundingSuggestions = new();

        public static void Draw(PoseTuneRoot root)
        {
            root.enableHeightAdjust = EditorGUILayout.Toggle("高さ調整を有効化", root.enableHeightAdjust);
            var height = root.GetComponentInChildren<PoseHeightAdjust>(true);
            if (height == null && GUILayout.Button("高さ調整を追加"))
            {
                var go = new GameObject("高さ調整");
                Undo.RegisterCreatedObjectUndo(go, "PoseTune 高さ調整を追加");
                go.transform.SetParent(root.transform, false);
                height = go.AddComponent<PoseHeightAdjust>();
            }

            if (height != null)
            {
                var editor = UnityEditor.Editor.CreateEditor(height);
                editor.OnInspectorGUI();
                UnityEngine.Object.DestroyImmediate(editor);
            }

            DrawGroundingAssist(root);
        }

        private static void DrawGroundingAssist(PoseTuneRoot root)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("接地補正", EditorStyles.boldLabel);
            var id = root.GetInstanceID();
            GroundingPoseSelections.TryGetValue(id, out var pose);
            pose = (PoseClip)EditorGUILayout.ObjectField("対象 Pose", pose, typeof(PoseClip), true);
            GroundingPoseSelections[id] = pose;

            using (new EditorGUI.DisabledScope(pose == null))
            {
                if (GUILayout.Button("接地補正候補を計算"))
                {
                    GroundingSuggestions[id] = new PoseGroundingAnalyzer().Analyze(
                        pose,
                        root,
                        new PoseGroundingAnalyzeOptions());
                }
            }

            if (!GroundingSuggestions.TryGetValue(id, out var suggestion) || suggestion == null)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                $"Root Y: {suggestion.SuggestedRootYOffset:0.###}\nCamera: {suggestion.SuggestedCameraOffset}\n{suggestion.Reason}",
                suggestion.RequiresManualReview ? MessageType.Warning : MessageType.Info);
            using (new EditorGUI.DisabledScope(pose == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Pose に適用"))
                    {
                        Undo.RecordObject(pose, "PoseTune 接地補正を適用");
                        pose.rootOffset = new Vector3(pose.rootOffset.x, suggestion.SuggestedRootYOffset, pose.rootOffset.z);
                        pose.cameraOffset = suggestion.SuggestedCameraOffset;
                        EditorUtility.SetDirty(pose);
                    }

                    if (GUILayout.Button("調整プリセットとして保存"))
                    {
                        var avatar = root.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
                        var captured = PoseGroundingPresetFactory.CreateAdjustmentPreset(
                            avatar != null ? avatar.gameObject.name : "",
                            pose,
                            suggestion);
                        var target = PoseTuneAssistantAssetFactory.Create<AvatarAdjustmentPreset>(root, "GroundingAdjustmentPreset");
                        EditorUtility.CopySerialized(captured, target);
                        UnityEngine.Object.DestroyImmediate(captured);
                        EditorUtility.SetDirty(target);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
    }
}
