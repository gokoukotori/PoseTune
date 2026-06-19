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
            var existingCost = ExistingExpressionParameterCost(graph);
            var totalCost = existingCost + plan.SyncedCost;
            if (totalCost > MaxExpressionParameterCost)
            {
                report.Error(PoseTuneDiagnostics.ParameterSyncedBudgetExceeded.Code,
                    $"PoseTune の同期パラメータコストは {plan.SyncedCost}、既存 Expression Parameter のコストは {existingCost} で、合計 {totalCost}/{MaxExpressionParameterCost} です。",
                    graph.RootComponent);
            }

            var generatedCount = plan.Parameters.Count(PoseTuneValidationContext.CountsAsExpressionParameter);
            var existingCount = ExistingExpressionParameterCount(graph);
            var totalCount = existingCount + generatedCount;
            if (totalCount > MaxExpressionParameterCount)
            {
                report.Error(PoseTuneDiagnostics.ExpressionParameterCountExceeded.Code,
                    $"PoseTune の Expression Parameter 数は {generatedCount}、既存 Expression Parameter 数は {existingCount} で、合計 {totalCount}/{MaxExpressionParameterCount} です。",
                    graph.RootComponent);
            }
            else if (totalCount > ExpressionParameterCountWarningThreshold)
            {
                report.Warning(PoseTuneDiagnostics.ExpressionParameterCountNearLimit.Code,
                    $"Expression Parameter 数が {totalCount}/{MaxExpressionParameterCount} です。VRChat の上限に近づいています。",
                    graph.RootComponent);
            }
        }

        private static int ExistingExpressionParameterCost(PoseGraph graph)
        {
            var descriptor = graph.AvatarDescriptor;
            if (descriptor == null)
            {
                return 0;
            }

            var expressionParameters = descriptor.GetType().GetField("expressionParameters")?.GetValue(descriptor);
            var parameters = expressionParameters?.GetType().GetField("parameters")?.GetValue(expressionParameters)
                as System.Collections.IEnumerable;
            if (parameters == null)
            {
                return 0;
            }

            var cost = 0;
            foreach (var parameter in parameters)
            {
                if (parameter == null || !ReadBoolField(parameter, "networkSynced", true))
                {
                    continue;
                }

                cost += CostForExpressionParameter(parameter);
            }

            return cost;
        }

        private static int ExistingExpressionParameterCount(PoseGraph graph)
        {
            var descriptor = graph.AvatarDescriptor;
            if (descriptor == null)
            {
                return 0;
            }

            var expressionParameters = descriptor.GetType().GetField("expressionParameters")?.GetValue(descriptor);
            var parameters = expressionParameters?.GetType().GetField("parameters")?.GetValue(expressionParameters)
                as System.Collections.IEnumerable;
            if (parameters == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var parameter in parameters)
            {
                if (parameter != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CostForExpressionParameter(object parameter)
        {
            var valueType = parameter.GetType().GetField("valueType")?.GetValue(parameter)?.ToString();
            return valueType == "Bool" ? 1 : 8;
        }

        private static bool ReadBoolField(object target, string fieldName, bool fallback)
        {
            var field = target.GetType().GetField(fieldName);
            return field != null ? (bool)field.GetValue(target) : fallback;
        }
    }
}
