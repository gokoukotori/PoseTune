using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AvatarScaleCorrectionCompiler
    {
        public static IReadOnlyList<(string Name, AnimatorControllerParameterType Type)> RequiredBuiltInParameters(
            PoseHeightAdjust height)
        {
            if (height == null)
            {
                return System.Array.Empty<(string, AnimatorControllerParameterType)>();
            }

            switch (height.autoCorrectionMode)
            {
                case HeightAutoCorrectionMode.RuntimeScaleFactor:
                    return new (string, AnimatorControllerParameterType)[]
                    {
                        ("ScaleFactor", AnimatorControllerParameterType.Float),
                        ("ScaleFactorInverse", AnimatorControllerParameterType.Float),
                        ("ScaleModified", AnimatorControllerParameterType.Bool)
                    };
                case HeightAutoCorrectionMode.RuntimeEyeHeightMeters:
                    return new (string, AnimatorControllerParameterType)[]
                    {
                        ("EyeHeightAsMeters", AnimatorControllerParameterType.Float),
                        ("EyeHeightAsPercent", AnimatorControllerParameterType.Float)
                    };
                default:
                    return System.Array.Empty<(string, AnimatorControllerParameterType)>();
            }
        }

        public static void EnsureBuiltInParameters(AnimatorController controller, PoseHeightAdjust height)
        {
            if (controller == null)
            {
                return;
            }

            foreach (var parameter in RequiredBuiltInParameters(height))
            {
                EnsureParameter(controller, parameter.Name, parameter.Type);
            }
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
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
