using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class ParameterPlanBuilder
    {
        private readonly ParameterPlan plan = new();

        public ParameterPlan Build()
        {
            return plan;
        }

        public ParameterDefinition Find(string name)
        {
            return plan.Find(name);
        }

        public ParameterPlanEntryBuilder AddBool(string name)
        {
            return Add(name, PoseTuneParameterSyncType.Bool, PoseTuneParameterValueType.Bool, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddInt(string name)
        {
            return Add(name, PoseTuneParameterSyncType.Int, PoseTuneParameterValueType.Int, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddFloat(string name)
        {
            return Add(name, PoseTuneParameterSyncType.Float, PoseTuneParameterValueType.Float, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddNotSyncedBool(string name)
        {
            return Add(name, PoseTuneParameterSyncType.NotSynced, PoseTuneParameterValueType.Bool, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddNotSyncedInt(string name)
        {
            return Add(name, PoseTuneParameterSyncType.NotSynced, PoseTuneParameterValueType.Int, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddNotSyncedFloat(string name)
        {
            return Add(name, PoseTuneParameterSyncType.NotSynced, PoseTuneParameterValueType.Float, recordDuplicate: true);
        }

        public ParameterPlanEntryBuilder AddBoolIfMissing(string name)
        {
            return Add(name, PoseTuneParameterSyncType.Bool, PoseTuneParameterValueType.Bool, recordDuplicate: false);
        }

        public ParameterPlanEntryBuilder AddFloatIfMissing(string name)
        {
            return Add(name, PoseTuneParameterSyncType.Float, PoseTuneParameterValueType.Float, recordDuplicate: false);
        }

        public ParameterPlanEntryBuilder AddNotSyncedFloatIfMissing(string name)
        {
            return Add(name, PoseTuneParameterSyncType.NotSynced, PoseTuneParameterValueType.Float, recordDuplicate: false);
        }

        private ParameterPlanEntryBuilder Add(
            string name,
            PoseTuneParameterSyncType syncType,
            PoseTuneParameterValueType valueType,
            bool recordDuplicate)
        {
            if (plan.Parameters.Any(p => p.Name == name))
            {
                if (recordDuplicate && !plan.DuplicateParameterNames.Contains(name))
                {
                    plan.DuplicateParameterNames.Add(name);
                }

                return ParameterPlanEntryBuilder.Noop;
            }

            var definition = new ParameterDefinition
            {
                Name = name,
                ValueType = valueType,
                SyncType = syncType
            };
            plan.Parameters.Add(definition);
            return new ParameterPlanEntryBuilder(definition);
        }
    }

    internal sealed class ParameterPlanEntryBuilder
    {
        internal static readonly ParameterPlanEntryBuilder Noop = new(null);

        private readonly ParameterDefinition definition;

        internal ParameterPlanEntryBuilder(ParameterDefinition definition)
        {
            this.definition = definition;
        }

        public ParameterPlanEntryBuilder Saved(bool value = true)
        {
            if (definition != null)
            {
                definition.Saved = value;
            }

            return this;
        }

        public ParameterPlanEntryBuilder LocalOnly(bool value = true)
        {
            if (definition != null)
            {
                definition.LocalOnly = value;
            }

            return this;
        }

        public ParameterPlanEntryBuilder AnimatorOnly(bool value = true)
        {
            if (definition != null)
            {
                definition.AnimatorOnly = value;
            }

            return this;
        }

        public ParameterPlanEntryBuilder DefaultValue(float value)
        {
            if (definition != null)
            {
                definition.DefaultValue = value;
            }

            return this;
        }
    }
}
