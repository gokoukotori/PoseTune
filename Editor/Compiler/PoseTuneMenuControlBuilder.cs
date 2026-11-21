using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneMenuControlBuilder
    {
        internal const string PageLabel = "ページ";

        internal static bool AutoSplit(PoseGraph graph)
        {
            return graph.Menu == null || graph.Menu.autoSplitMenu;
        }

        internal static bool GeneratesHeightRadial(PoseGraph graph)
        {
            return ParameterAllocator.NeedsGeneratedHeightParameter(graph) &&
                   graph.HeightAdjust != null &&
                   graph.HeightAdjust.generateRadialMenu;
        }

        internal static MenuControlPlan Radial(string label, string parameter)
        {
            return new MenuControlPlan
            {
                Label = label,
                Type = PoseTuneMenuControlType.RadialPuppet,
                Parameter = "",
                SubParameters = new List<string> { parameter }
            };
        }

        public static MenuControlPlan BuildMotionTimeMenu(PoseGraph graph)
        {
            var controls = new List<MenuControlPlan>();
            var seenParameters = new HashSet<string>();
            var heightParameterAlreadyControlled = GeneratesHeightRadial(graph)
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

                controls.Add(Radial(pose.DisplayName + " 時間", resolution.ParameterName));
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
                    Toggle("頭をロック", root.Parameter(PoseTuneNames.LockHead), 1),
                    Toggle("手をロック", root.Parameter(PoseTuneNames.LockHands), 1),
                    Toggle("足をロック", root.Parameter(PoseTuneNames.LockFeet), 1),
                    Toggle("移動ロック", root.Parameter(PoseTuneNames.LocomotionLock), 1)
                }
            };
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

                controls.Add(BuildGroupMenu(group, AutoSplit(graph), poseSelection));
            }

            if (PoseTuneCompilerRules.NeedsSupineFlag(graph))
            {
                controls.Add(BuildSupineToggle(graph));
            }

            menu.Children.AddRange(PaginateControls(controls, PageLabel, AutoSplit(graph)));
            return menu;
        }

        public static MenuControlPlan BuildSupineToggle(PoseGraph graph)
        {
            return Toggle("仰向け切替", graph.RootComponent.Parameter(PoseTuneNames.SupineFlag), 1);
        }

        public static MenuControlPlan BuildModeMenu(PoseGraph graph)
        {
            var parameter = graph.RootComponent.Parameter(PoseTuneNames.Mode);
            var mode = new MenuControlPlan { Label = "モード", Type = PoseTuneMenuControlType.SubMenu };
            mode.Children.Add(Toggle("オフ", parameter, PoseTuneModeValues.Off));
            if (graph.RootComponent.enableAutoContextSwitch)
            {
                mode.Children.Add(Toggle("自動", parameter, PoseTuneModeValues.Auto));
            }

            mode.Children.Add(Toggle("手動", parameter, PoseTuneModeValues.Manual));
            return mode;
        }

        public static List<MenuControlPlan> BuildFlatGroupControls(
            PoseGroupDefinition group,
            PoseSelectionPlan poseSelection)
        {
            var groupBinding = poseSelection?.Find(group);
            var controls = new List<MenuControlPlan>
            {
                Toggle(group.DisplayName + " オフ", groupBinding?.ParameterName ?? group.ParameterName, 0, group.Icon)
            };
            controls.AddRange(PoseToggles(group, poseSelection));
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
                Toggle("オフ", groupBinding?.ParameterName ?? group.ParameterName, 0)
            };
            controls.AddRange(PoseToggles(group, poseSelection));
            menu.Children.AddRange(PaginateControls(controls, PageLabel, split));

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

        private static IEnumerable<MenuControlPlan> PoseToggles(
            PoseGroupDefinition group,
            PoseSelectionPlan poseSelection)
        {
            foreach (var pose in group.Poses)
            {
                var binding = poseSelection?.Find(pose);
                yield return Toggle(
                    pose.DisplayName,
                    binding?.ParameterName ?? group.ParameterName,
                    binding?.Value ?? SelectionValue(group, pose),
                    pose.Icon);
            }
        }

        private static MenuControlPlan Toggle(string label, string parameter, float value, Texture2D icon = null)
        {
            return new MenuControlPlan
            {
                Label = label,
                Type = PoseTuneMenuControlType.Toggle,
                Parameter = parameter,
                Value = value,
                Icon = icon
            };
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
