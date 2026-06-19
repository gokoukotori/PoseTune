using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneImportSelectionState
    {
        private readonly List<ImportCandidate> candidates = new();
        private readonly List<bool> selected = new();

        public int Count => candidates.Count;
        public IReadOnlyList<ImportCandidate> Candidates => candidates;

        public void SetCandidates(IEnumerable<ImportCandidate> nextCandidates)
        {
            candidates.Clear();
            candidates.AddRange(nextCandidates ?? Enumerable.Empty<ImportCandidate>());
            selected.Clear();
            selected.AddRange(candidates.Select(candidate => candidate.EnabledByDefault));
        }

        public bool IsSelected(int index)
        {
            return index >= 0 && index < selected.Count && selected[index];
        }

        public void SetSelected(int index, bool value)
        {
            if (index < 0 || index >= selected.Count)
            {
                return;
            }

            selected[index] = value;
        }

        public IEnumerable<ImportCandidate> SelectedCandidates()
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (IsSelected(i))
                {
                    yield return candidates[i];
                }
            }
        }
    }
}
