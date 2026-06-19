using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantPoseTab
    {
        public static void Draw(PoseTuneRoot root)
        {
            EditorGUILayout.LabelField("グループ", EditorStyles.boldLabel);
            for (var i = 0; i < PoseTuneAssistantUiModel.AddablePoseGroupKinds.Length; i += 3)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var j = i; j < Mathf.Min(i + 3, PoseTuneAssistantUiModel.AddablePoseGroupKinds.Length); j++)
                    {
                        var kind = PoseTuneAssistantUiModel.AddablePoseGroupKinds[j];
                        if (GUILayout.Button(PoseTuneAssistantUiModel.AddPoseGroupLabel(kind)))
                        {
                            PoseTuneAuthoringFactory.AddPoseGroup(root, kind);
                        }
                    }
                }
            }

            var groups = root.GetComponentsInChildren<PoseGroup>(true).OrderBy(g => g.menuOrder).ToArray();
            foreach (var group in groups)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField(group, typeof(PoseGroup), true);
                    if (GUILayout.Button("ポーズ追加", GUILayout.Width(90)))
                    {
                        PoseTuneAuthoringFactory.AddPoseClip(group, null);
                    }
                }
            }
        }
    }
}
