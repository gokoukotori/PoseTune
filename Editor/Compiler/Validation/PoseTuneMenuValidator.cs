using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneMenuValidator
    {
        public static void Validate(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            foreach (var group in graph.Groups)
            {
                ValidateMenuValues(graph.RootComponent, group, report);
            }

            ValidateMenuControlLimit(context, report);
            ValidateMenuInstallMode(graph, report);
        }

        private static void ValidateMenuControlLimit(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            if (graph.Menu == null || graph.Menu.installMode == MenuInstallMode.None)
            {
                return;
            }

            var menu = context.Menu;
            if (graph.Menu.installMode == MenuInstallMode.InlineAtRoot)
            {
                var existingRootControlCount = ExistingRootMenuControlCount(graph);
                var generatedInlineControlCount = menu.Root.Children.Count;
                if (existingRootControlCount + generatedInlineControlCount > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    report.Error(PoseTuneDiagnostics.MenuControlLimitExceeded.Code,
                        $"InlineAtRoot では既存 Avatar root menu の {existingRootControlCount} controls と PoseTune の {generatedInlineControlCount} controls の合計が 8 を超えます。",
                        graph.Menu);
                    return;
                }
            }

            if (graph.Menu.autoSplitMenu)
            {
                return;
            }

            foreach (var control in MenuPages(menu.Root))
            {
                if (control.Children.Count > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    report.Error(PoseTuneDiagnostics.MenuControlLimitExceeded.Code,
                        "autoSplitMenu が無効な状態で、PoseTune メニューが 8 コントロールを超えています。",
                        graph.Menu);
                    return;
                }
            }
        }

        private static int ExistingRootMenuControlCount(PoseGraph graph)
        {
            var rootMenu = graph.AvatarDescriptor != null ? graph.AvatarDescriptor.expressionsMenu : null;
            return rootMenu != null ? rootMenu.controls.Count(control => control != null) : 0;
        }

        private static IEnumerable<MenuControlPlan> MenuPages(MenuControlPlan control)
        {
            yield return control;
            foreach (var child in control.Children)
            {
                foreach (var page in MenuPages(child))
                {
                    yield return page;
                }
            }
        }

        private static void ValidateMenuInstallMode(PoseGraph graph, ValidationReport report)
        {
            if (graph.Menu == null)
            {
                return;
            }

            if (graph.Menu.installMode == MenuInstallMode.None &&
                PoseGraphBuildFilter.BuildableGroups(graph)
                    .Any(group => PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group)))
            {
                report.Warning(PoseTuneDiagnostics.ManualControlMenuMissing.Code, "menu が生成されないため manual control できません。", graph.Menu);
            }
        }

        private static void ValidateMenuValues(PoseTuneRoot root, PoseGroupDefinition group, ValidationReport report)
        {
            foreach (var pose in group.Poses)
            {
                var selectionValue = pose.SelectionValue(root);
                if (selectionValue <= 0)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "PoseClip の選択値は 0 より大きい必要があります。", pose.Source);
                }

                if (selectionValue > 255)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "PoseClip の選択値は 255 以下である必要があります。", pose.Source);
                }
            }

            foreach (var duplicate in group.Poses.GroupBy(p => p.SelectionValue(root)).Where(g => g.Key > 0 && g.Count() > 1))
            {
                foreach (var pose in duplicate)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "グループ内で PoseClip の選択値が重複しています: " + duplicate.Key, pose.Source);
                }
            }
        }
    }
}
