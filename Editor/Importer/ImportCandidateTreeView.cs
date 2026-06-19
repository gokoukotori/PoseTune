using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class ImportCandidateTreeRow
    {
        public int Depth;
        public string Label = "";
        public ImportCandidate Candidate;
    }

    internal static class ImportCandidateTreeView
    {
        public static IReadOnlyList<ImportCandidateTreeRow> Rows(IEnumerable<ImportCandidate> candidates)
        {
            var rows = new List<ImportCandidateTreeRow>();
            foreach (var layer in (candidates ?? Enumerable.Empty<ImportCandidate>())
                         .GroupBy(candidate => string.IsNullOrWhiteSpace(candidate.SourceLayerName)
                             ? "Unknown"
                             : candidate.SourceLayerName))
            {
                rows.Add(new ImportCandidateTreeRow { Depth = 0, Label = "Layer: " + layer.Key });
                foreach (var candidate in layer.OrderBy(candidate => candidate.StatePath))
                {
                    rows.Add(new ImportCandidateTreeRow { Depth = 1, Label = "State: " + candidate.StatePath });
                    if (candidate.BlendTreePath.Count > 0)
                    {
                        rows.AddRange(candidate.BlendTreePath.Select(info => new ImportCandidateTreeRow
                        {
                            Depth = 2,
                            Label = "BlendTree: " + info.BlendTreeName + " / " + info.BlendParameter + " = " + info.Threshold.ToString("0.###")
                        }));
                    }

                    rows.Add(new ImportCandidateTreeRow
                    {
                        Depth = 3,
                        Label = candidate.DisplayName + "  confidence " + UnityEngine.Mathf.RoundToInt(candidate.Confidence * 100f) + "%",
                        Candidate = candidate
                    });
                }
            }

            return rows;
        }

        public static void Draw(IEnumerable<ImportCandidate> candidates)
        {
            foreach (var row in Rows(candidates))
            {
                EditorGUILayout.LabelField(new string(' ', row.Depth * 2) + row.Label);
            }
        }
    }
}
