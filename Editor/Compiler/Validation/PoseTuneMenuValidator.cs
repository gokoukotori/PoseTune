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
            foreach (var channel in context.Parameters.PoseSelection.Channels)
            {
                ValidateMenuValues(channel, report);
            }

            foreach (var group in graph.Groups.Where(group =>
                         context.Parameters.PoseSelection.Find(group) == null))
            {
                ValidateLegacyMenuValues(graph.RootComponent, group, report);
            }

            ValidateMenuControlLimit(context, report);
            ValidateMenuInstallMode(graph, report);
        }

        private static void ValidateMenuControlLimit(PoseTuneValidationContext context, ValidationReport report)
        {
            var graph = context.Graph;
            var installMode = graph.Menu != null ? graph.Menu.installMode : MenuInstallMode.AppendToRoot;
            if (installMode == MenuInstallMode.None)
            {
                return;
            }

            var menu = context.Menu;
            var existingRootControlCount = ExistingRootMenuControlCount(graph);
            if (installMode == MenuInstallMode.InlineAtRoot)
            {
                var generatedInlineControlCount = menu.Root.Children.Count;
                if (existingRootControlCount + generatedInlineControlCount > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    report.Error(PoseTuneDiagnostics.MenuControlLimitExceeded.Code,
                        $"InlineAtRoot では既存 Avatar root menu の {existingRootControlCount} controls と PoseTune の {generatedInlineControlCount} controls の合計が 8 を超えます。",
                        graph.Menu != null ? graph.Menu : graph.RootComponent);
                    return;
                }
            }
            else if (existingRootControlCount + 1 > VRCExpressionsMenu.MAX_CONTROLS)
            {
                report.Error(PoseTuneDiagnostics.MenuControlLimitExceeded.Code,
                    $"AppendToRoot では既存 Avatar root menu の {existingRootControlCount} controls に PoseTune submenu 1 つを追加するため、合計が 8 を超えます。",
                    graph.Menu != null ? graph.Menu : graph.RootComponent);
                return;
            }

            var autoSplitMenu = graph.Menu == null || graph.Menu.autoSplitMenu;
            if (autoSplitMenu)
            {
                return;
            }

            foreach (var control in MenuPages(menu.Root))
            {
                if (control.Children.Count > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    report.Error(PoseTuneDiagnostics.MenuControlLimitExceeded.Code,
                        "autoSplitMenu が無効な状態で、PoseTune メニューが 8 コントロールを超えています。",
                        graph.Menu != null ? graph.Menu : graph.RootComponent);
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

        private static void ValidateMenuValues(PoseSelectionChannel channel, ValidationReport report)
        {
            foreach (var binding in channel.Poses)
            {
                var selectionValue = binding.Value;
                if (selectionValue <= 0)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "PoseClip の選択値は 0 より大きい必要があります。", binding.Pose.Source);
                }

                if (selectionValue > 255)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "PoseClip の選択値は 255 以下である必要があります。", binding.Pose.Source);
                }
            }

            foreach (var duplicate in channel.Poses.GroupBy(binding => binding.Value)
                         .Where(group => group.Key > 0 && group.Count() > 1))
            {
                foreach (var binding in duplicate)
                {
                    report.Error(PoseTuneDiagnostics.ClipMenuValueInvalid.Code, "同じ選択パラメータ内で PoseClip の値が重複しています: " + duplicate.Key, binding.Pose.Source);
                }
            }
        }

        private static void ValidateLegacyMenuValues(
            PoseTuneRoot root,
            PoseGroupDefinition group,
            ValidationReport report)
        {
            foreach (var pose in group.Poses)
            {
                var selectionValue = pose.SelectionValue(root);
                if (selectionValue <= 0 || selectionValue > 255)
                {
                    report.Error(
                        PoseTuneDiagnostics.ClipMenuValueInvalid.Code,
                        "PoseClip の選択値は 1 以上 255 以下である必要があります。",
                        pose.Source);
                }
            }

            foreach (var duplicate in group.Poses.GroupBy(pose => pose.SelectionValue(root))
                         .Where(values => values.Key > 0 && values.Count() > 1))
            {
                foreach (var pose in duplicate)
                {
                    report.Error(
                        PoseTuneDiagnostics.ClipMenuValueInvalid.Code,
                        "グループ内で PoseClip の選択値が重複しています: " + duplicate.Key,
                        pose.Source);
                }
            }
        }
    }
}
