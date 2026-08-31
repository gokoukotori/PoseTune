using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PosePreviewWindowRow
    {
        public PoseGroupKind GroupKind;
        public string GroupName = "";
        public PoseClip Pose;
        public string TrackingSummary = "";
    }

    internal static class PosePreviewWindowModel
    {
        public static IReadOnlyList<PosePreviewWindowRow> CollectRows(
            PoseTuneRoot root,
            string search,
            PoseGroupKind? groupFilter)
        {
            if (root == null)
            {
                return Array.Empty<PosePreviewWindowRow>();
            }

            var graph = new PoseGraphCollector().Collect(root);
            var query = search ?? "";
            return graph.Poses
                .Where(pose => pose.Source != null)
                .Where(pose => !groupFilter.HasValue || pose.Group.Kind == groupFilter.Value)
                .Where(pose => string.IsNullOrWhiteSpace(query) ||
                               pose.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               pose.Group.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(pose => new PosePreviewWindowRow
                {
                    GroupKind = pose.Group.Kind,
                    GroupName = pose.Group.DisplayName,
                    Pose = pose.Source,
                    TrackingSummary = TrackingSummary(pose.Group.TrackingPolicy)
                })
                .ToList();
        }

        private static string TrackingSummary(TrackingPolicyData policy)
        {
            policy ??= TrackingPolicyData.DefaultForPose();
            return $"Head:{policy.head} Hands:{policy.leftHand}/{policy.rightHand} Hip:{policy.hip} Feet:{policy.leftFoot}/{policy.rightFoot}";
        }
    }

    public sealed class PosePreviewWindow : EditorWindow
    {
        private PoseTuneRoot selectedRoot;
        private PoseClip selectedPose;
        private string search = "";
        private PoseGroupKind groupFilter = PoseGroupKind.Standing;
        private bool filterByGroup;
        private bool resetPreviewOnClose = true;
        private Vector2 scroll;

        [MenuItem("Tools/PoseTune/Pose Preview Window")]
        public static void Open()
        {
            GetWindow<PosePreviewWindow>("Pose Preview");
        }

        private void OnGUI()
        {
            DrawRootSelector();
            DrawFilters();
            DrawPoseList();
            DrawPreviewControls();
        }

        private void DrawRootSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedRoot = (PoseTuneRoot)EditorGUILayout.ObjectField("Root", selectedRoot, typeof(PoseTuneRoot), true);
                if (GUILayout.Button("Use Selection", GUILayout.Width(110)))
                {
                    selectedRoot = Selection.activeGameObject != null
                        ? Selection.activeGameObject.GetComponentInParent<PoseTuneRoot>(true)
                        : null;
                }
            }
        }

        private void DrawFilters()
        {
            search = EditorGUILayout.TextField("Search", search);
            using (new EditorGUILayout.HorizontalScope())
            {
                filterByGroup = EditorGUILayout.ToggleLeft("Filter Group", filterByGroup, GUILayout.Width(110));
                using (new EditorGUI.DisabledScope(!filterByGroup))
                {
                    groupFilter = (PoseGroupKind)EditorGUILayout.EnumPopup(groupFilter);
                }
            }

            resetPreviewOnClose = EditorGUILayout.ToggleLeft("Reset Preview On Close", resetPreviewOnClose);
        }

        private void DrawPoseList()
        {
            var rows = PosePreviewWindowModel.CollectRows(
                selectedRoot,
                search,
                filterByGroup ? groupFilter : null);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160));
            foreach (var row in rows)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Toggle(selectedPose == row.Pose, "", GUILayout.Width(18)))
                    {
                        selectedPose = row.Pose;
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(row.GroupName + " / " + row.Pose.displayName);
                        EditorGUILayout.LabelField(row.TrackingSummary, EditorStyles.miniLabel);
                    }

                    using (new EditorGUI.DisabledScope(row.Pose == null))
                    {
                        if (GUILayout.Button("Preview", GUILayout.Width(80)))
                        {
                            selectedPose = row.Pose;
                            PosePreviewController.ApplyPreview(row.Pose);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selectedPose == null))
                {
                    if (GUILayout.Button("Apply Preview"))
                    {
                        PosePreviewController.ApplyPreview(selectedPose);
                    }

                }

                if (GUILayout.Button("Reset Preview"))
                {
                    PosePreviewController.ResetPreview();
                }
            }
        }

        private void OnDisable()
        {
            if (resetPreviewOnClose)
            {
                PosePreviewController.ResetPreview();
            }
        }
    }
}
