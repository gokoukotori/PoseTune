using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ParameterPlan
    {
        public List<ParameterDefinition> Parameters = new();
        public List<string> DuplicateParameterNames = new();

        public int SyncedCost => Parameters.Where(p => !p.LocalOnly && p.SyncType != PoseTuneParameterSyncType.NotSynced)
            .Sum(p => p.SyncType == PoseTuneParameterSyncType.Bool ? 1 : 8);

        public IEnumerable<ParameterDefinition> SyncedParameters =>
            Parameters.Where(p => !p.LocalOnly && p.SyncType != PoseTuneParameterSyncType.NotSynced);

        public ParameterDefinition Find(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name == name);
        }
    }

    public sealed class ParameterDefinition
    {
        public string Name = "";
        public PoseTuneParameterValueType ValueType = PoseTuneParameterValueType.Float;
        public PoseTuneParameterSyncType SyncType;
        public bool Saved;
        public bool LocalOnly;
        public bool AnimatorOnly;
        public float DefaultValue;
    }

    public enum PoseTuneParameterValueType
    {
        Bool,
        Int,
        Float
    }
}
