using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class ImportConditionFoldoutView
    {
        private static readonly HashSet<string> Expanded = new();

        public void Draw(ImportCandidate candidate, PoseTuneImportSelectionState state, int index)
        {
            if (candidate == null || candidate.ConditionBranchInfos.Count == 0)
            {
                return;
            }

            var key = candidate.AnimatorPath + ":" + index;
            var expanded = Expanded.Contains(key);
            expanded = EditorGUILayout.Foldout(expanded, "Conditions", true);
            if (expanded)
            {
                Expanded.Add(key);
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var branch in candidate.ConditionBranchInfos)
                    {
                        EditorGUILayout.LabelField(branch.Source);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            if (branch.Conditions.Count == 0)
                            {
                                EditorGUILayout.LabelField("<unconditional>");
                                continue;
                            }

                            foreach (var condition in branch.Conditions)
                            {
                                EditorGUILayout.LabelField(condition.parameter + " " + condition.op);
                            }
                        }
                    }
                }
            }
            else
            {
                Expanded.Remove(key);
            }
        }
    }
}
