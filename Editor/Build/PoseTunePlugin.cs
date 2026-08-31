using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.builtin;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(Gokoukotori.PoseTune.Editor.PoseTunePlugin))]

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTunePlugin : Plugin<PoseTunePlugin>
    {
        public const string PluginQualifiedName = "com.gokoukotori.posetune";

        public override string QualifiedName => PluginQualifiedName;
        public override string DisplayName => "PoseTune";
        public override Color? ThemeColor => new Color(0.18f, 0.64f, 0.72f, 1f);

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .Run("PoseTune authoring を収集して検証", PoseTuneBuildRunner.CollectAndValidate)
                .BeforePass(RemoveEditorOnlyPass.Instance);

            InPhase(BuildPhase.Generating)
                .Run("PoseTune assets と Modular Avatar components を生成", PoseTuneBuildRunner.Generate)
                .BeforePlugin("nadena.dev.modular-avatar");

            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .WithRequiredExtension(typeof(AnimatorServicesContext), sequence =>
                    sequence.Run("PoseTune の生成 build output を検証", PoseTuneBuildRunner.PostValidate));

            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("PoseTune helper components を削除", PoseTuneBuildRunner.Cleanup);
        }
    }

    internal static class PoseTuneBuildRunner
    {
        public static void CollectAndValidate(BuildContext context)
        {
            var state = context.GetState<PoseTuneBuildState>();
            state.Graphs.Clear();
            state.Reports.Clear();

            foreach (var root in context.AvatarRootObject.GetComponentsInChildren<PoseTuneRoot>(true))
            {
                var graph = new PoseGraphCollector().Collect(root);
                new PoseTuneIconResolver().Apply(graph);
                var report = new PoseValidator().Validate(graph);
                graph.Validation = report;
                state.Graphs.Add(graph);
                state.Reports.Add(report);
                LogReport(root, report);
            }
        }

        public static void Generate(BuildContext context)
        {
            var state = context.GetState<PoseTuneBuildState>();
            if (state.Graphs.Count == 0)
            {
                CollectAndValidate(context);
            }

            RecollectGraphsForGeneration(context, state);
            ModularAvatarEmitter.ClearAllGeneratedObjects(context.AvatarRootObject);

            foreach (var graph in state.Graphs)
            {
                if (graph.Root == null || graph.HasErrors)
                {
                    continue;
                }

                var parameterPlan = new ParameterAllocator().AllocateStrict(graph);
                var animatorResult = new AnimatorCompiler().Compile(graph, parameterPlan);
                var animatorValidation = new PoseTuneAnimatorValidator().Validate(
                    graph,
                    animatorResult.TargetController,
                    parameterPlan);
                MergeReport(graph.Validation, animatorValidation);
                if (animatorValidation.Issues.Any())
                {
                    LogReport(graph.RootComponent, animatorValidation);
                }

                if (animatorValidation.HasErrors)
                {
                    PoseTuneGeneratedAnimatorAssetCleanup.DestroyUnsaved(animatorResult);
                    continue;
                }

                var saved = PoseTuneNdmfAssetSaver.TrySaveGeneratedAnimatorAssets(context, animatorResult);
                if (!saved)
                {
                    graph.Validation.Error(
                        PoseTuneDiagnostics.BuildGeneratedAnimatorAssetsSaveFailed.Code,
                        "NDMF AssetSaver が利用できないため、PoseTune が生成した animator assets を build 用に保存できません。",
                        graph.RootComponent);
                    LogReport(graph.RootComponent, graph.Validation);
                    PoseTuneGeneratedAnimatorAssetCleanup.DestroyUnsaved(animatorResult);
                    continue;
                }

                var menuPlan = new MenuCompiler().Compile(graph, parameterPlan);
                ModularAvatarEmitter.Emit(graph, parameterPlan, menuPlan, animatorResult);
            }
        }

        public static void PostValidate(BuildContext context)
        {
            var state = context.GetState<PoseTuneBuildState>();
            var postBuildValidator = new PoseTunePostBuildValidator();
            var virtualControllers = context.Extension<AnimatorServicesContext>().ControllerContext;
            foreach (var graph in state.Graphs.Where(g => g.Root != null && !g.HasErrors))
            {
                var report = postBuildValidator.Validate(graph, virtualControllers);
                state.Reports.Add(report);
                LogReport(graph.Root, report);
            }
        }

        public static void Cleanup(BuildContext context)
        {
            PoseTuneBuildCleanup.CleanupAuthoringForBuild(context.AvatarRootObject);
        }

        private static void RecollectGraphsForGeneration(BuildContext context, PoseTuneBuildState state)
        {
            var alreadyReported = new HashSet<string>(
                state.Reports.SelectMany(report => report.Issues).Select(IssueKey));
            state.Graphs.Clear();
            state.Reports.Clear();

            foreach (var root in context.AvatarRootObject.GetComponentsInChildren<PoseTuneRoot>(true))
            {
                var graph = new PoseGraphCollector().Collect(root);
                new PoseTuneIconResolver().Apply(graph);
                var report = new PoseValidator().Validate(graph);
                graph.Validation = report;
                state.Graphs.Add(graph);
                state.Reports.Add(report);
                LogNewReportIssues(root, report, alreadyReported);
            }
        }

        private static void LogNewReportIssues(Object context, ValidationReport report, HashSet<string> alreadyReported)
        {
            var filtered = new ValidationReport();
            foreach (var issue in report.Issues)
            {
                if (!alreadyReported.Add(IssueKey(issue)))
                {
                    continue;
                }

                filtered.Add(issue);
            }

            if (filtered.Issues.Any())
            {
                LogReport(context, filtered);
            }
        }

        private static string IssueKey(ValidationIssue issue)
        {
            return $"{issue.Severity}|{issue.Code}|{issue.Message}";
        }

        private static void MergeReport(ValidationReport destination, ValidationReport source)
        {
            if (destination == null || source == null)
            {
                return;
            }

            foreach (var issue in source.Issues)
            {
                destination.Add(issue);
            }
        }

        private static void LogReport(Object context, ValidationReport report)
        {
            PoseTuneNdmfErrorReporter.Report(report, context);
        }
    }
}
