using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseClipSelectionPreview
    {
        private const string SelectedPosePreviewMenuPath = "Tools/PoseTune/Settings/Selected Pose Preview";
        private const int SelectedPosePreviewMenuPriority = 1100;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Selection.selectionChanged -= RefreshFromSelection;
            Selection.selectionChanged += RefreshFromSelection;
            PoseTuneEditorProjectSettings.EnableSelectedPosePreviewChanged -= OnEnableSelectedPosePreviewChanged;
            PoseTuneEditorProjectSettings.EnableSelectedPosePreviewChanged += OnEnableSelectedPosePreviewChanged;
        }

        public static void RefreshFromSelection()
        {
            if (!PoseTuneEditorProjectSettings.EnableSelectedPosePreview)
            {
                PosePreviewController.ResetPreview();
                return;
            }

            var selections = Selection.objects;
            if (selections.Length != 1)
            {
                PosePreviewController.ResetPreview();
                return;
            }

            var pose = SelectedPose(selections[0]);
            if (pose == null)
            {
                PosePreviewController.ResetPreview();
                return;
            }

            PosePreviewController.ApplyPreview(pose);
        }

        [MenuItem(SelectedPosePreviewMenuPath, true)]
        private static bool ValidateSelectedPosePreview()
        {
            Menu.SetChecked(SelectedPosePreviewMenuPath, PoseTuneEditorProjectSettings.EnableSelectedPosePreview);
            return true;
        }

        [MenuItem(SelectedPosePreviewMenuPath, false, SelectedPosePreviewMenuPriority)]
        private static void ToggleSelectedPosePreview()
        {
            PoseTuneEditorProjectSettings.EnableSelectedPosePreview =
                !PoseTuneEditorProjectSettings.EnableSelectedPosePreview;
        }

        private static void OnEnableSelectedPosePreviewChanged(bool enabled)
        {
            if (enabled)
            {
                RefreshFromSelection();
            }
            else
            {
                PosePreviewController.ResetPreview();
            }
        }

        private static PoseClip SelectedPose(Object selected)
        {
            if (selected is PoseClip pose)
            {
                return pose;
            }

            if (selected is GameObject gameObject)
            {
                return gameObject.GetComponent<PoseClip>();
            }

            if (selected is Component component)
            {
                return component.GetComponent<PoseClip>();
            }

            return null;
        }
    }
}
