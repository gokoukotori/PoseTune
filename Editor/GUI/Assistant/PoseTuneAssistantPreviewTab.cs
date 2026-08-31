using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantPreviewTab
    {
        public static void Draw(PoseTuneRoot root)
        {
            root.questLowMemoryMode = EditorGUILayout.Toggle("Quest / 低メモリモード", root.questLowMemoryMode);

            foreach (var pose in root.GetComponentsInChildren<PoseClip>(true).OrderBy(p => p.menuOrder))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField(pose.displayName, pose, typeof(PoseClip), true);
                    if (GUILayout.Button("プレビュー", GUILayout.Width(80)))
                    {
                        PosePreviewController.ApplyPreview(pose);
                    }

                }
            }

            if (GUILayout.Button("プレビューをリセット"))
            {
                PosePreviewController.ResetPreview();
            }
        }
    }
}
