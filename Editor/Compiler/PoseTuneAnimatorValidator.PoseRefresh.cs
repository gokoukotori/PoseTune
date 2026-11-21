using System;
using System.Linq;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class PoseTuneAnimatorValidator
    {
        private static void ValidatePoseRefresh(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            if (!PoseRefreshLayerCompiler.IsNeeded(graph))
            {
                return;
            }

            ValidatePoseRefreshLayer(graph, controller, report);
            if (!PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent))
            {
                return;
            }

            const string actionWeightLayerName = PoseTuneNames.ActionWeightLayerName;
            var actionWeight = FindLayer(controller, actionWeightLayerName);
            var active = FindState(actionWeight, PoseTuneNames.ActionWeightActiveState);
            if (active == null)
            {
                report.Error(
                    PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                    $"Action weight state が見つかりません: {actionWeightLayerName}/{PoseTuneNames.ActionWeightActiveState}",
                    graph.RootComponent);
                return;
            }

            ValidateRefreshLoop(active, actionWeightLayerName, graph.RootComponent, report);
        }

        private static void ValidateRefreshLoop(
            StateView state,
            string layerName,
            UnityEngine.Object context,
            ValidationReport report)
        {
            if (state == null)
            {
                return;
            }

            if ((state.transitions ?? Array.Empty<TransitionView>()).Any(transition =>
                    transition != null &&
                    transition.destinationState == state &&
                    HasCondition(transition, PoseTuneNames.PoseRefreshParameter, AnimatorConditionMode.If, 0f)))
            {
                return;
            }

            report.Error(
                PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                $"{PoseTuneNames.PoseRefreshParameter} の再適用遷移がありません: {layerName}/{state.name}",
                context);
        }

        private static void ValidatePoseRefreshLayer(
            PoseGraph graph,
            ControllerView controller,
            ValidationReport report)
        {
            var layerName = PoseTuneNames.PoseRefreshLayerName;
            var layer = FindLayer(controller, layerName);
            var states = layer?.stateMachine?.states ?? Array.Empty<StateView>();
            var raises = states.Where(state => state != null && ParametersSetBy(state).Any(parameter =>
                    parameter.name == PoseTuneNames.PoseRefreshParameter && parameter.value > 0.5f))
                .ToList();

            bool RaisesOn(string parameter, AnimatorConditionMode mode)
            {
                return states.Any(state => (state?.transitions ?? Array.Empty<TransitionView>())
                    .Any(transition =>
                        transition != null &&
                        raises.Contains(transition.destinationState) &&
                        HasCondition(transition, parameter, mode, 0f)));
            }

            var clears = states.Any(state => state != null && ParametersSetBy(state).Any(parameter =>
                parameter.name == PoseTuneNames.PoseRefreshParameter && parameter.value < 0.5f));
            if (layer?.stateMachine != null &&
                RaisesOn("Grounded", AnimatorConditionMode.IfNot) &&
                RaisesOn("VRCEmote", AnimatorConditionMode.NotEqual) &&
                RaisesOn("AFK", AnimatorConditionMode.If) &&
                clears)
            {
                return;
            }

            report.Error(
                PoseTuneDiagnostics.AnimatorTrackingResetStateMissing.Code,
                $"Jump／エモート／AFK 後の再適用 layer がないか不正です: {layerName}",
                graph.RootComponent);
        }
    }
}
