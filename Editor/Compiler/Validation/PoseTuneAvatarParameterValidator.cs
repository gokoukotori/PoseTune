using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneAvatarParameterValidator
    {
        private const int MaxExpressionParameterCost = 256;
        private const int MaxExpressionParameterCount = 8192;
        private const int ExpressionParameterCountWarningThreshold = 8000;

        public static void Validate(PoseTuneValidationContext context, ValidationReport report)
        {
            ValidateExistingExpressionParameterConflicts(context, report);
            ValidateExistingAnimatorParameterConflicts(context, report);
            ValidateSyncedBudget(context, report);
        }

        private static void ValidateExistingExpressionParameterConflicts(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            var descriptor = graph.AvatarDescriptor;
            if (descriptor == null)
            {
                return;
            }

            var expressionParameters = descriptor.GetType().GetField("expressionParameters")?.GetValue(descriptor);
            var parameters = expressionParameters?.GetType().GetField("parameters")?.GetValue(expressionParameters)
                as System.Collections.IEnumerable;
            if (parameters == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                var parameterType = parameter.GetType();
                var name = parameterType.GetField("name")?.GetValue(parameter)?.ToString();
                if (string.IsNullOrWhiteSpace(name) ||
                    !context.ExpectedExpressionParameterTypes.TryGetValue(name, out var expectedType))
                {
                    continue;
                }

                var actualType = parameterType.GetField("valueType")?.GetValue(parameter)?.ToString();
                if (actualType != expectedType)
                {
                    report.Error(PoseTuneDiagnostics.ExistingExpressionParameterTypeConflict.Code,
                        $"既存 Avatar Expression Parameter の型が異なります: {name} ({actualType} != {expectedType})。",
                        graph.RootComponent);
                }
            }
        }

        private static void ValidateExistingAnimatorParameterConflicts(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            if (graph.AvatarDescriptor == null)
            {
                return;
            }

            foreach (var controller in ExistingAnimatorControllers(graph))
            {
                foreach (var parameter in controller.parameters)
                {
                    if (parameter == null ||
                        !context.ExpectedAnimatorParameterTypes.TryGetValue(parameter.name, out var expectedType))
                    {
                        continue;
                    }

                    if (parameter.type != expectedType)
                    {
                        report.Error(PoseTuneDiagnostics.ExistingAnimatorParameterTypeConflict.Code,
                            $"既存 Avatar Animator パラメータの型が異なります: {parameter.name} ({parameter.type} != {expectedType})。",
                            graph.RootComponent);
                    }
                }
            }
        }

        private static IEnumerable<AnimatorController> ExistingAnimatorControllers(PoseGraph graph)
        {
            var descriptor = graph.AvatarDescriptor;
            if (descriptor == null)
            {
                yield break;
            }

            var layers = Enumerable.Empty<VRCAvatarDescriptor.CustomAnimLayer>();
            if (descriptor.baseAnimationLayers != null)
            {
                layers = layers.Concat(descriptor.baseAnimationLayers);
            }

            if (descriptor.specialAnimationLayers != null)
            {
                layers = layers.Concat(descriptor.specialAnimationLayers);
            }

            var seen = new HashSet<AnimatorController>();
            foreach (var layer in layers)
            {
                if (!layer.isEnabled || layer.animatorController == null)
                {
                    continue;
                }

                if (layer.animatorController is AnimatorController controller && seen.Add(controller))
                {
                    yield return controller;
                }
            }
        }

        private static void ValidateSyncedBudget(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            var plan = context.Parameters;
            var budget = ExpressionParameterBudgetCalculator.Calculate(
                ExistingExpressionParameters(graph),
                plan.Parameters);
            if (budget.TotalSyncedCost > MaxExpressionParameterCost)
            {
                report.Error(PoseTuneDiagnostics.ParameterSyncedBudgetExceeded.Code,
                    $"PoseTune が追加する同期パラメータコストは {budget.AdditionalSyncedCost}、既存 Expression Parameter のコストは {budget.ExistingSyncedCost} で、マージ後の合計は {budget.TotalSyncedCost}/{MaxExpressionParameterCost} です。",
                    graph.RootComponent);
            }

            if (budget.TotalCount > MaxExpressionParameterCount)
            {
                report.Error(PoseTuneDiagnostics.ExpressionParameterCountExceeded.Code,
                    $"PoseTune が追加する Expression Parameter 数は {budget.AdditionalCount}、既存 Expression Parameter 数は {budget.ExistingCount} で、マージ後の合計は {budget.TotalCount}/{MaxExpressionParameterCount} です。",
                    graph.RootComponent);
            }
            else if (budget.TotalCount > ExpressionParameterCountWarningThreshold)
            {
                report.Warning(PoseTuneDiagnostics.ExpressionParameterCountNearLimit.Code,
                    $"Expression Parameter 数が {budget.TotalCount}/{MaxExpressionParameterCount} です。VRChat の上限に近づいています。",
                    graph.RootComponent);
            }
        }

        private static IEnumerable<ExistingExpressionParameterSnapshot> ExistingExpressionParameters(PoseGraph graph)
        {
            var descriptor = graph.AvatarDescriptor;
            if (descriptor == null)
            {
                yield break;
            }

            var expressionParameters = descriptor.GetType().GetField("expressionParameters")?.GetValue(descriptor);
            var parameters = expressionParameters?.GetType().GetField("parameters")?.GetValue(expressionParameters)
                as System.Collections.IEnumerable;
            if (parameters == null)
            {
                yield break;
            }

            foreach (var parameter in parameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                var parameterType = parameter.GetType();
                var name = parameterType.GetField("name")?.GetValue(parameter)?.ToString() ?? "";
                var valueType = ToPoseTuneValueType(
                    parameterType.GetField("valueType")?.GetValue(parameter)?.ToString());
                yield return new ExistingExpressionParameterSnapshot(
                    name,
                    valueType,
                    ReadBoolField(parameter, "networkSynced", true));
            }
        }

        private static PoseTuneParameterValueType ToPoseTuneValueType(string valueType)
        {
            switch (valueType)
            {
                case "Bool":
                    return PoseTuneParameterValueType.Bool;
                case "Int":
                    return PoseTuneParameterValueType.Int;
                default:
                    return PoseTuneParameterValueType.Float;
            }
        }

        private static bool ReadBoolField(object target, string fieldName, bool fallback)
        {
            var field = target.GetType().GetField(fieldName);
            return field != null ? (bool)field.GetValue(target) : fallback;
        }
    }
}
