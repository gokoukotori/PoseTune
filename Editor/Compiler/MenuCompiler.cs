using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class MenuCompiler
    {
        public MenuPlan Compile(PoseGraph graph, ParameterPlan parameters)
        {
            var poseSelection = parameters?.PoseSelection ?? PoseSelectionPlanner.Build(graph);
            var root = new MenuControlPlan
            {
                Label = graph.Menu != null && !string.IsNullOrWhiteSpace(graph.Menu.rootMenuName) ? graph.Menu.rootMenuName : "PoseTune",
                Type = PoseTuneMenuControlType.SubMenu
            };

            var controls = new List<MenuControlPlan>
            {
                PoseTuneMenuControlBuilder.BuildModeMenu(graph)
            };
            var optionsMenu = PoseTuneMenuControlBuilder.BuildOptionsMenu(graph);
            if (optionsMenu != null)
            {
                controls.Add(optionsMenu);
            }

            var useSubMenusPerGroup = graph.Menu == null || graph.Menu.useSubMenusPerGroup;
            var autoSplitMenu = PoseTuneMenuControlBuilder.AutoSplit(graph);
            var buildableGroups = PoseGraphBuildFilter.BuildableGroups(graph).ToList();
            if (useSubMenusPerGroup)
            {
                var separateLyingGroups = graph.Menu != null &&
                                          graph.Menu.lyingMenuLayout == LyingMenuLayout.SeparateGroups;
                foreach (var group in buildableGroups)
                {
                    if (PoseTuneCompilerRules.IsLyingGroup(group) && !separateLyingGroups)
                    {
                        continue;
                    }

                    if (!PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
                    {
                        continue;
                    }

                    controls.Add(PoseTuneMenuControlBuilder.BuildGroupMenu(group, autoSplitMenu, poseSelection));
                }

                var lyingGroups = buildableGroups.FindAll(PoseTuneCompilerRules.IsLyingGroup);
                if (!separateLyingGroups && lyingGroups.Count > 0)
                {
                    controls.Add(PoseTuneMenuControlBuilder.BuildLyingMenu(graph, lyingGroups, poseSelection));
                }
                else if (separateLyingGroups && PoseTuneCompilerRules.NeedsSupineFlag(graph))
                {
                    controls.Add(PoseTuneMenuControlBuilder.BuildSupineToggle(graph));
                }
            }
            else
            {
                foreach (var group in buildableGroups)
                {
                    if (!PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
                    {
                        continue;
                    }

                    controls.AddRange(PoseTuneMenuControlBuilder.BuildFlatGroupControls(group, poseSelection));
                }

                if (PoseTuneCompilerRules.NeedsSupineFlag(graph))
                {
                    controls.Add(PoseTuneMenuControlBuilder.BuildSupineToggle(graph));
                }
            }

            var motionTimeMenu = PoseTuneMenuControlBuilder.BuildMotionTimeMenu(graph);
            if (motionTimeMenu != null)
            {
                controls.Add(motionTimeMenu);
            }

            if (PoseTuneMenuControlBuilder.GeneratesHeightRadial(graph))
            {
                controls.Add(PoseTuneMenuControlBuilder.Radial(
                    "高さ",
                    PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust)));
            }

            root.Children.AddRange(PoseTuneMenuControlBuilder.PaginateControls(
                controls,
                PoseTuneMenuControlBuilder.PageLabel,
                autoSplitMenu));

            return new MenuPlan { Root = root };
        }
    }
}
