using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneMenuControlBuilder
    {
        public static MenuControlPlan BuildMotionTimeMenu(PoseGraph graph)
        {
            var controls = new List<MenuControlPlan>();
            var seenParameters = new HashSet<string>();
            var heightParameterAlreadyControlled = ParameterAllocator.NeedsGeneratedHeightParameter(graph) &&
                                                   graph.HeightAdjust != null &&
                                                   graph.HeightAdjust.generateRadialMenu
                ? PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust)
                : "";
            foreach (var pose in graph.Poses
                         .OrderBy(pose => pose.Group?.MenuOrder ?? 0)
                         .ThenBy(pose => pose.MenuOrder)
                         .ThenBy(pose => pose.DisplayName))
            {
                if (pose.MotionTime == null || !pose.MotionTime.generateRadialMenu)
                {
                    continue;
                }

                var resolution = MotionTimeParameterResolver.Resolve(
                    graph,
                    pose,
                    MotionTimeParameterUsage.RadialMenu);
                if (!resolution.HasParameter ||
                    resolution.ParameterName == heightParameterAlreadyControlled ||
                    !seenParameters.Add(resolution.ParameterName))
                {
                    continue;
                }

                controls.Add(new MenuControlPlan
                {
                    Label = pose.DisplayName + " 時間",
                    Type = PoseTuneMenuControlType.RadialPuppet,
                    Parameter = "",
                    SubParameters = new List<string> { resolution.ParameterName }
                });
            }

            if (controls.Count == 0)
            {
                return null;
            }

            return new MenuControlPlan
            {
                Label = "Motion Time",
                Type = PoseTuneMenuControlType.SubMenu,
                Children = controls
            };
        }

        public static MenuControlPlan BuildOptionsMenu(PoseGraph graph)
        {
            if (!graph.HasPoseOptions)
            {
                return null;
            }

            var root = graph.RootComponent;
            return new MenuControlPlan
            {
                Label = "オプション",
                Type = PoseTuneMenuControlType.SubMenu,
                Children = new List<MenuControlPlan>
                {
                    new()
                    {
                        Label = "頭をロック",
                        Type = PoseTuneMenuControlType.Toggle,
                        Parameter = root.Parameter(PoseTuneNames.LockHead),
                        Value = 1
                    },
                    new()
                    {
                        Label = "手をロック",
                        Type = PoseTuneMenuControlType.Toggle,
                        Parameter = root.Parameter(PoseTuneNames.LockHands),
                        Value = 1
                    },
                    new()
                    {
                        Label = "足をロック",
                        Type = PoseTuneMenuControlType.Toggle,
                        Parameter = root.Parameter(PoseTuneNames.LockFeet),
                        Value = 1
                    },
                    new()
                    {
                        Label = "移動ロック",
                        Type = PoseTuneMenuControlType.Toggle,
                        Parameter = root.Parameter(PoseTuneNames.LocomotionLock),
                        Value = 1
                    }
                }
            };
        }

        public static bool NeedsSupineToggle(PoseGraph graph)
        {
            return graph.RootComponent.enableAutoContextSwitch &&
                   PoseGraphBuildFilter.BuildableGroups(graph)
                       .Any(g => g.Kind == PoseGroupKind.Prone || g.Kind == PoseGroupKind.Supine);
        }

        public static bool IsLyingGroup(PoseGroupDefinition group)
        {
            return group.Kind == PoseGroupKind.Prone || group.Kind == PoseGroupKind.Supine;
        }

        public static MenuControlPlan BuildLyingMenu(
            PoseGraph graph,
            List<PoseGroupDefinition> groups,
            PoseSelectionPlan poseSelection)
        {
            var menu = new MenuControlPlan
            {
                Label = "寝姿勢",
                Type = PoseTuneMenuControlType.SubMenu
            };
            var controls = new List<MenuControlPlan>();
            foreach (var group in groups)
            {
                if (!PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
                {
                    continue;
                }

                controls.Add(BuildGroupMenu(
                    group,
                    graph.Menu == null || graph.Menu.autoSplitMenu,
                    poseSelection));
            }

            if (NeedsSupineToggle(graph))
            {
                controls.Add(BuildSupineToggle(graph));
            }

            menu.Children.AddRange(PaginateControls(controls, "ページ", graph.Menu == null || graph.Menu.autoSplitMenu));
            return menu;
        }

        public static MenuControlPlan BuildSupineToggle(PoseGraph graph)
        {
            return new MenuControlPlan
            {
                Label = "仰向け切替",
                Type = PoseTuneMenuControlType.Toggle,
                Parameter = graph.RootComponent.Parameter(PoseTuneNames.SupineFlag),
                Value = 1
            };
        }

        public static MenuControlPlan BuildModeMenu(PoseGraph graph)
        {
            var parameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var mode = new MenuControlPlan { Label = "モード", Type = PoseTuneMenuControlType.SubMenu };
            mode.Children.Add(new MenuControlPlan { Label = "オフ", Type = PoseTuneMenuControlType.Toggle, Parameter = parameter, Value = 0 });
            if (graph.RootComponent.enableAutoContextSwitch)
            {
                mode.Children.Add(new MenuControlPlan { Label = "自動", Type = PoseTuneMenuControlType.Toggle, Parameter = parameter, Value = 1 });
            }

            mode.Children.Add(new MenuControlPlan { Label = "手動", Type = PoseTuneMenuControlType.Toggle, Parameter = parameter, Value = 2 });
            return mode;
        }

        public static List<MenuControlPlan> BuildFlatGroupControls(
            PoseGroupDefinition group,
            PoseSelectionPlan poseSelection)
        {
            var groupBinding = poseSelection?.Find(group);
            var controls = new List<MenuControlPlan>
            {
                new MenuControlPlan
                {
                    Label = group.DisplayName + " オフ",
                    Type = PoseTuneMenuControlType.Toggle,
                    Parameter = groupBinding?.ParameterName ?? group.ParameterName,
                    Value = 0,
                    Icon = group.Icon
                }
            };
            foreach (var pose in group.Poses)
            {
                controls.Add(new MenuControlPlan
                {
                    Label = pose.DisplayName,
                    Type = PoseTuneMenuControlType.Toggle,
                    Parameter = poseSelection?.Find(pose)?.ParameterName ?? group.ParameterName,
                    Value = poseSelection?.Find(pose)?.Value ?? SelectionValue(group, pose),
                    Icon = pose.Icon
                });
            }

            return controls;
        }

        public static MenuControlPlan BuildGroupMenu(
            PoseGroupDefinition group,
            bool split,
            PoseSelectionPlan poseSelection)
        {
            var groupBinding = poseSelection?.Find(group);
            var menu = new MenuControlPlan
            {
                Label = group.DisplayName,
                Type = PoseTuneMenuControlType.SubMenu,
                Icon = group.Icon
            };

            var controls = new List<MenuControlPlan>
            {
                new MenuControlPlan
                {
                    Label = "オフ",
                    Type = PoseTuneMenuControlType.Toggle,
                    Parameter = groupBinding?.ParameterName ?? group.ParameterName,
                    Value = 0
                }
            };
            foreach (var pose in group.Poses)
            {
                controls.Add(new MenuControlPlan
                {
                    Label = pose.DisplayName,
                    Type = PoseTuneMenuControlType.Toggle,
                    Parameter = poseSelection?.Find(pose)?.ParameterName ?? group.ParameterName,
                    Value = poseSelection?.Find(pose)?.Value ?? SelectionValue(group, pose),
                    Icon = pose.Icon
                });
            }

            menu.Children.AddRange(PaginateControls(controls, "ページ", split));

            return menu;
        }

        public static List<MenuControlPlan> PaginateControls(List<MenuControlPlan> controls, string labelPrefix, bool split)
        {
            if (!split || controls.Count <= VRCExpressionsMenu.MAX_CONTROLS)
            {
                return controls;
            }

            var pages = new List<MenuControlPlan>();
            var pageIndex = 1;
            for (var i = 0; i < controls.Count; i += VRCExpressionsMenu.MAX_CONTROLS)
            {
                var page = new MenuControlPlan
                {
                    Label = labelPrefix + " " + pageIndex++,
                    Type = PoseTuneMenuControlType.SubMenu
                };
                page.Children.AddRange(controls.GetRange(i, System.Math.Min(VRCExpressionsMenu.MAX_CONTROLS, controls.Count - i)));
                pages.Add(page);
            }

            return pages.Count <= VRCExpressionsMenu.MAX_CONTROLS
                ? pages
                : PaginateControls(pages, labelPrefix, true);
        }

        private static int SelectionValue(PoseGroupDefinition group, PoseDefinition pose)
        {
            var root = group?.Source != null
                ? group.Source.GetComponentInParent<PoseTuneRoot>(true)
                : null;
            return pose != null ? pose.SelectionValue(root) : 0;
        }
    }
}
