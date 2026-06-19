using System;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Hashing;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    public static class ModularAvatarEmitter
    {
        public static GameObject Emit(PoseGraph graph, ParameterPlan parameters, MenuPlan menuPlan, AnimatorBuildResult animators)
        {
            var generatedRoot = CreateGeneratedRoot(graph);
            EmitParameters(generatedRoot, parameters);
            EmitMenu(generatedRoot, graph, menuPlan);
            var matchAvatarWriteDefaults = graph.RootComponent.poseWriteDefaultsMode != PoseWriteDefaultsMode.ForceOff;
            EmitAnimator(generatedRoot, animators.TargetController, graph.RootComponent.targetLayer == PoseTuneTargetLayer.Base
                ? VRCAvatarDescriptor.AnimLayerType.Base
                : VRCAvatarDescriptor.AnimLayerType.Action,
                matchAvatarWriteDefaults,
                TargetLayerPriority(graph));
            if (HasAnimatorContent(animators.FxController))
            {
                EmitAnimator(generatedRoot, animators.FxController, VRCAvatarDescriptor.AnimLayerType.FX, matchAvatarWriteDefaults);
            }
            return generatedRoot;
        }

        public static void ClearGeneratedObjects(PoseGraph graph)
        {
            PoseTuneGeneratedObjectCleaner.ClearGeneratedObjects(graph);
        }

        public static void ClearAllGeneratedObjects(GameObject avatarRoot)
        {
            PoseTuneGeneratedObjectCleaner.ClearAllGeneratedObjects(avatarRoot);
        }

        private static GameObject CreateGeneratedRoot(PoseGraph graph)
        {
            ClearGeneratedObjects(graph);

            var go = new GameObject(PoseTuneNames.GeneratedRootName);
            go.transform.SetParent(graph.AvatarRoot.transform, false);
            var markerComponent = go.AddComponent<PoseTuneGeneratedMarker>();
            markerComponent.rootGuid = graph.RootComponent.StableGuid;
            markerComponent.generatedVersion = PoseTunePackageInfo.Version;
            markerComponent.graphHash = PoseTuneGraphHasher.Compute(graph);
            markerComponent.generatedAt = DateTime.UtcNow.ToString("O");
            return go;
        }

        private static void EmitParameters(GameObject parent, ParameterPlan plan)
        {
            var component = parent.AddComponent<ModularAvatarParameters>();
            foreach (var parameter in plan.Parameters)
            {
                component.parameters.Add(new ParameterConfig
                {
                    nameOrPrefix = parameter.Name,
                    remapTo = "",
                    internalParameter = parameter.AnimatorOnly,
                    isPrefix = false,
                    syncType = ToMaSyncType(parameter.SyncType),
                    localOnly = parameter.LocalOnly,
                    saved = parameter.Saved,
                    defaultValue = parameter.DefaultValue,
                    hasExplicitDefaultValue = Math.Abs(parameter.DefaultValue) > 0.0001f
                });
            }
        }

        private static ParameterSyncType ToMaSyncType(PoseTuneParameterSyncType type)
        {
            switch (type)
            {
                case PoseTuneParameterSyncType.Bool:
                    return ParameterSyncType.Bool;
                case PoseTuneParameterSyncType.Int:
                    return ParameterSyncType.Int;
                case PoseTuneParameterSyncType.Float:
                    return ParameterSyncType.Float;
                default:
                    return ParameterSyncType.NotSynced;
            }
        }

        private static void EmitAnimator(
            GameObject parent,
            RuntimeAnimatorController controller,
            VRCAvatarDescriptor.AnimLayerType layerType,
            bool matchAvatarWriteDefaults,
            int layerPriority = 0)
        {
            var go = new GameObject("Merge " + layerType);
            go.transform.SetParent(parent.transform, false);
            var merge = go.AddComponent<ModularAvatarMergeAnimator>();
            merge.animator = controller;
            merge.layerType = layerType;
            merge.mergeAnimatorMode = MergeAnimatorMode.Append;
            merge.pathMode = MergeAnimatorPathMode.Absolute;
            merge.deleteAttachedAnimator = false;
            merge.matchAvatarWriteDefaults = matchAvatarWriteDefaults;
            merge.layerPriority = layerPriority;
        }

        private static int TargetLayerPriority(PoseGraph graph)
        {
            var compatibility = graph?.GoroneSystemExCompatibility;
            return graph != null &&
                   graph.HasGoroneSystemExGuard &&
                   compatibility != null &&
                   compatibility.overridePoseTuneLayerPriority
                ? compatibility.poseTuneLayerPriority
                : 0;
        }

        private static bool HasAnimatorContent(RuntimeAnimatorController controller)
        {
            return controller is AnimatorController animatorController &&
                   animatorController.layers.Length > 0;
        }

        private static void EmitMenu(GameObject parent, PoseGraph graph, MenuPlan plan)
        {
            var installMode = graph.Menu != null ? graph.Menu.installMode : MenuInstallMode.AppendToRoot;
            if (installMode == MenuInstallMode.None)
            {
                return;
            }

            var menuRoot = new GameObject(plan.Root.Label);
            menuRoot.transform.SetParent(parent.transform, false);
            menuRoot.AddComponent<ModularAvatarMenuInstaller>();
            if (installMode == MenuInstallMode.InlineAtRoot)
            {
                EmitChildren(menuRoot.transform, plan.Root);
                return;
            }

            var item = menuRoot.AddComponent<ModularAvatarMenuItem>();
            item.Control = ToVrcControl(plan.Root);
            item.MenuSource = SubmenuSource.Children;
            EmitChildren(menuRoot.transform, plan.Root);
        }

        private static void EmitChildren(Transform parent, MenuControlPlan plan)
        {
            foreach (var childPlan in plan.Children)
            {
                var go = new GameObject(childPlan.Label);
                go.transform.SetParent(parent, false);
                var item = go.AddComponent<ModularAvatarMenuItem>();
                item.Control = ToVrcControl(childPlan);
                item.MenuSource = childPlan.Type == PoseTuneMenuControlType.SubMenu
                    ? SubmenuSource.Children
                    : SubmenuSource.MenuAsset;
                EmitChildren(go.transform, childPlan);
            }
        }

        private static VRCExpressionsMenu.Control ToVrcControl(MenuControlPlan plan)
        {
            var control = new VRCExpressionsMenu.Control
            {
                name = plan.Label,
                icon = plan.Icon,
                type = ToVrcType(plan.Type),
                parameter = new VRCExpressionsMenu.Control.Parameter { name = plan.Parameter },
                value = plan.Value
            };
            if (plan.SubParameters.Count > 0)
            {
                control.subParameters = new VRCExpressionsMenu.Control.Parameter[plan.SubParameters.Count];
                for (var i = 0; i < plan.SubParameters.Count; i++)
                {
                    control.subParameters[i] = new VRCExpressionsMenu.Control.Parameter { name = plan.SubParameters[i] };
                }
            }

            return control;
        }

        private static VRCExpressionsMenu.Control.ControlType ToVrcType(PoseTuneMenuControlType type)
        {
            switch (type)
            {
                case PoseTuneMenuControlType.Button:
                    return VRCExpressionsMenu.Control.ControlType.Button;
                case PoseTuneMenuControlType.Toggle:
                    return VRCExpressionsMenu.Control.ControlType.Toggle;
                case PoseTuneMenuControlType.RadialPuppet:
                    return VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                default:
                    return VRCExpressionsMenu.Control.ControlType.SubMenu;
            }
        }

    }

}

