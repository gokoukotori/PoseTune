using System;
using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal readonly struct ExistingExpressionParameterSnapshot
    {
        public ExistingExpressionParameterSnapshot(
            string name,
            PoseTuneParameterValueType valueType,
            bool networkSynced)
        {
            Name = name ?? "";
            ValueType = valueType;
            NetworkSynced = networkSynced;
        }

        public string Name { get; }
        public PoseTuneParameterValueType ValueType { get; }
        public bool NetworkSynced { get; }
    }

    internal readonly struct ExpressionParameterBudget
    {
        public ExpressionParameterBudget(
            int existingCount,
            int additionalCount,
            int existingSyncedCost,
            int additionalSyncedCost)
        {
            ExistingCount = existingCount;
            AdditionalCount = additionalCount;
            ExistingSyncedCost = existingSyncedCost;
            AdditionalSyncedCost = additionalSyncedCost;
        }

        public int ExistingCount { get; }
        public int AdditionalCount { get; }
        public int TotalCount => ExistingCount + AdditionalCount;
        public int ExistingSyncedCost { get; }
        public int AdditionalSyncedCost { get; }
        public int TotalSyncedCost => ExistingSyncedCost + AdditionalSyncedCost;
    }

    internal static class ExpressionParameterBudgetCalculator
    {
        public static ExpressionParameterBudget Calculate(
            IEnumerable<ExistingExpressionParameterSnapshot> existingParameters,
            IEnumerable<ParameterDefinition> generatedParameters)
        {
            var existing = (existingParameters ?? Enumerable.Empty<ExistingExpressionParameterSnapshot>())
                .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(group => new ExistingExpressionParameterSnapshot(
                    group.Key,
                    group.First().ValueType,
                    group.Any(parameter => parameter.NetworkSynced)))
                .ToList();
            var generatedByName = (generatedParameters ?? Enumerable.Empty<ParameterDefinition>())
                .Where(parameter => parameter != null && !parameter.AnimatorOnly && parameter.Name != null)
                .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var existingNames = new HashSet<string>(existing.Select(parameter => parameter.Name), StringComparer.Ordinal);

            var existingSyncedCost = existing
                .Where(parameter => parameter.NetworkSynced)
                .Sum(parameter => Cost(parameter.ValueType));

            var mergedExistingSyncedCost = existing.Sum(parameter =>
            {
                var generatedMakesNetworkSynced =
                    generatedByName.TryGetValue(parameter.Name, out var generated) &&
                    generated.SyncType != PoseTuneParameterSyncType.NotSynced &&
                    !generated.LocalOnly;
                return parameter.NetworkSynced || generatedMakesNetworkSynced
                    ? Cost(parameter.ValueType)
                    : 0;
            });

            var additionalParameters = generatedByName.Values
                .Where(parameter => parameter.SyncType != PoseTuneParameterSyncType.NotSynced &&
                                    !existingNames.Contains(parameter.Name))
                .ToList();
            var newSyncedCost = additionalParameters
                .Where(parameter => !parameter.LocalOnly)
                .Sum(parameter => Cost(parameter.ValueType));

            return new ExpressionParameterBudget(
                existing.Count,
                additionalParameters.Count,
                existingSyncedCost,
                mergedExistingSyncedCost - existingSyncedCost + newSyncedCost);
        }

        private static int Cost(PoseTuneParameterValueType valueType)
        {
            return valueType == PoseTuneParameterValueType.Bool ? 1 : 8;
        }
    }
}
