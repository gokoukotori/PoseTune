using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Hashing;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTunePostBuildValidator
    {
        public ValidationReport Validate(PoseGraph graph, VirtualControllerContext virtualControllers = null)
        {
            var report = new ValidationReport();
            if (graph?.RootComponent == null || graph.AvatarRoot == null)
            {
                report.Error(PoseTuneDiagnostics.BuildGraphContextMissing.Code, "PoseTune build graph に root/avatar context がありません。", graph?.RootComponent);
                return report;
            }

            var markers = graph.AvatarRoot.GetComponentsInChildren<PoseTuneGeneratedMarker>(true)
                .Where(marker => marker.rootKey == graph.RootId)
                .ToList();
            if (markers.Count == 0)
            {
                report.Warning(PoseTuneDiagnostics.BuildGeneratedMarkerMissing.Code, "PoseTune の生成マーカーが見つかりません。", graph.RootComponent);
            }
            else
            {
                var expectedHash = PoseTuneGraphHasher.Compute(graph);
                foreach (var marker in markers)
                {
                    ValidateMarker(marker, expectedHash, report);
                }
            }

            ValidateMergedOutputs(graph, virtualControllers, report);
            return report;
        }

        private static void ValidateMarker(
            PoseTuneGeneratedMarker marker,
            string expectedHash,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(marker.generatedVersion))
            {
                report.Error(PoseTuneDiagnostics.BuildGeneratedVersionMissing.Code, "PoseTune の生成出力に generatedVersion がありません。", marker);
            }

            if (string.IsNullOrWhiteSpace(marker.graphHash))
            {
                report.Error(PoseTuneDiagnostics.BuildGraphHashMissing.Code, "PoseTune の生成出力に graphHash がありません。", marker);
            }
            else if (marker.graphHash != expectedHash)
            {
                report.Warning(PoseTuneDiagnostics.BuildGraphHashMismatch.Code, "PoseTune の生成出力 graphHash が現在の graph と一致しません。", marker);
            }
        }

        private static void ValidateMergedOutputs(
            PoseGraph graph,
            VirtualControllerContext virtualControllers,
            ValidationReport report)
        {
            var descriptor = graph.AvatarDescriptor;
            var parameters = new ParameterAllocator().Allocate(graph);
            foreach (var parameter in parameters.Parameters.Where(parameter => !parameter.AnimatorOnly))
            {
                if (!HasExpressionParameter(descriptor.expressionParameters, parameter.Name))
                {
                    report.Error(PoseTuneDiagnostics.BuildExpressionParameterMissing.Code, "最終 Expression Parameters に PoseTune パラメータがありません: " + parameter.Name,
                        graph.RootComponent);
                    return;
                }
            }

            var modeParameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            if ((graph.Menu == null || graph.Menu.installMode != MenuInstallMode.None) &&
                !ExpressionMenuParameterSearch.Contains(descriptor.expressionsMenu, modeParameter))
            {
                report.Error(PoseTuneDiagnostics.BuildMenuControlMissing.Code, "最終 Expressions Menu に PoseTune メニューコントロールがありません。", graph.RootComponent);
            }

            var targetLayer = graph.RootComponent.targetLayer == PoseTuneTargetLayer.Base
                ? VRCAvatarDescriptor.AnimLayerType.Base
                : VRCAvatarDescriptor.AnimLayerType.Action;
            VirtualAnimatorController virtualController = null;
            if (virtualControllers != null)
            {
                if (!virtualControllers.Controllers.TryGetValue(targetLayer, out virtualController) ||
                    virtualController == null)
                {
                    report.Error(
                        PoseTuneDiagnostics.BuildPlayableLayerMissing.Code,
                        "NDMF Animator Services の最終 playable layer に PoseTune の対象 controller がありません。",
                        graph.RootComponent);
                    return;
                }

            }

            var targetController = virtualControllers == null
                ? ControllerForLayer(descriptor, targetLayer)
                : null;
            if (virtualController == null && targetController == null)
            {
                report.Error(PoseTuneDiagnostics.BuildPlayableLayerMissing.Code, "最終 Avatar playable layer に PoseTune のポーズレイヤーがありません。",
                    graph.RootComponent);
                return;
            }

            var layerNames = virtualController != null
                ? virtualController.Layers.Select(layer => layer.Name)
                : targetController.layers.Select(layer => layer.name);
            if (PoseGraphBuildFilter.BuildableGroups(graph)
                .SelectMany(PoseTuneLayerNaming.ExpectedLayerNames)
                .Any(layerName => !layerNames.Contains(layerName)))
            {
                report.Error(PoseTuneDiagnostics.BuildPlayableLayerMissing.Code, "最終 Avatar playable layer に PoseTune のポーズレイヤーがありません。",
                    graph.RootComponent);
            }

            var animatorValidator = new PoseTuneAnimatorValidator();
            var animatorReport = virtualController != null
                ? animatorValidator.Validate(graph, virtualController)
                : animatorValidator.Validate(graph, targetController);
            foreach (var issue in animatorReport.Issues)
            {
                report.Add(issue);
            }
        }

        private static bool HasExpressionParameter(VRCExpressionParameters parameters, string name)
        {
            return parameters != null &&
                   parameters.parameters != null &&
                   parameters.parameters.Any(parameter => parameter != null && parameter.name == name);
        }

        private static AnimatorController ControllerForLayer(
            VRCAvatarDescriptor descriptor,
            VRCAvatarDescriptor.AnimLayerType layerType)
        {
            return descriptor.baseAnimationLayers?
                .Concat(descriptor.specialAnimationLayers ?? new VRCAvatarDescriptor.CustomAnimLayer[0])
                .Where(layer => layer.type == layerType && !layer.isDefault)
                .Select(layer => layer.animatorController as AnimatorController)
                .FirstOrDefault(controller => controller != null);
        }
    }
}
