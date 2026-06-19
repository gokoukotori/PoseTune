using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorControllerFactory
    {
        public static AnimatorBuildResult CreateBuildResult(PoseGraph graph, ParameterPlan parameters)
        {
            var result = new AnimatorBuildResult
            {
                TargetController = CreateController(ControllerName(graph, "Target")),
                FxController = CreateController(ControllerName(graph, "FX"))
            };

            AddParameters(result.TargetController, parameters, graph);
            AddParameters(result.FxController, parameters, graph);
            return result;
        }

        private static AnimatorController CreateController(string name)
        {
            var controller = new AnimatorController { name = name };
            controller.layers = new AnimatorControllerLayer[0];
            return controller;
        }

        private static string ControllerName(PoseGraph graph, string layer)
        {
            var suffix = graph?.RootComponent != null
                ? PoseTuneNames.ShortGuid(graph.RootComponent.StableGuid)
                : "";
            return string.IsNullOrWhiteSpace(suffix)
                ? "PoseTune_" + layer
                : "PoseTune_" + layer + "_" + suffix;
        }

        private static void AddParameters(AnimatorController controller, ParameterPlan plan, PoseGraph graph)
        {
            foreach (var parameter in plan.Parameters)
            {
                var type = PoseTuneParameterTypeMapper.ToAnimatorType(parameter.ValueType);
                if (type == AnimatorControllerParameterType.Trigger)
                {
                    continue;
                }

                controller.AddParameter(new AnimatorControllerParameter
                {
                    name = parameter.Name,
                    type = type,
                    defaultBool = parameter.DefaultValue > 0.5f,
                    defaultFloat = parameter.DefaultValue,
                    defaultInt = Mathf.RoundToInt(parameter.DefaultValue)
                });
            }

            EnsureParameter(controller, "TrackingType", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "VRMode", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "Upright", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Seated", AnimatorControllerParameterType.Bool);
            AvatarScaleCorrectionCompiler.EnsureBuiltInParameters(controller, graph?.HeightAdjust);
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }
    }
}
