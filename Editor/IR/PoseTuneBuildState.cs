using System.Collections.Generic;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneBuildState
    {
        public List<PoseGraph> Graphs = new();
        public List<ValidationReport> Reports = new();
        public ValidationReport Validation = new();
    }
}
