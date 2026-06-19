using System;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class GoroneSystemExDetector
    {
        public const string VrcSupineParameter = "VRCSupine";
        public const string VrcSupineExAdjustParameter = "VRCSupineExAdjust";
        public const string VrcSupineExAdjustingParameter = "VRCSupineExAdjusting";

        public static GoroneSystemExDetectionResult Detect(GameObject avatarRoot)
        {
            var result = new GoroneSystemExDetectionResult();
            if (avatarRoot == null)
            {
                return result;
            }

            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                if (NameEquals(transform.name, "SupineMA_EX"))
                {
                    result.ExMarkers.Add(transform.gameObject);
                }
                else if (NameEquals(transform.name, "SupineMA"))
                {
                    result.SupineMarkers.Add(transform.gameObject);
                }
            }

            foreach (var parameters in avatarRoot.GetComponentsInChildren<ModularAvatarParameters>(true))
            {
                foreach (var parameter in parameters.parameters)
                {
                    if (parameter.isPrefix)
                    {
                        continue;
                    }

                    if (parameter.nameOrPrefix == VrcSupineParameter)
                    {
                        result.VrcSupineParameters.Add(new GoroneSystemExParameterHandle
                        {
                            Component = parameters,
                            Config = parameter
                        });
                    }

                    if (IsExParameter(parameter.nameOrPrefix))
                    {
                        result.ExMarkers.Add(parameters);
                    }
                    else if (IsSupineParameter(parameter.nameOrPrefix))
                    {
                        result.SupineMarkers.Add(parameters);
                    }
                }
            }

            foreach (var merge in avatarRoot.GetComponentsInChildren<ModularAvatarMergeAnimator>(true))
            {
                var animatorName = merge.animator != null ? merge.animator.name : "";
                if (Contains(animatorName, "SupineLocomotionEx"))
                {
                    result.ExMarkers.Add(merge);
                }
                else if (Contains(animatorName, "SupineLocomotion"))
                {
                    result.SupineMarkers.Add(merge);
                }
            }

            return result;
        }

        private static bool IsExParameter(string parameter)
        {
            return parameter == VrcSupineExAdjustParameter ||
                   parameter == VrcSupineExAdjustingParameter ||
                   parameter == "VRCSupineAutoRotation" ||
                   parameter == "VRCSupineHandSwitchable";
        }

        private static bool IsSupineParameter(string parameter)
        {
            return parameter == VrcSupineParameter ||
                   parameter == "VRCLockPose" ||
                   parameter == "VRCFootAnchor";
        }

        private static bool NameEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string value, string candidate)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal sealed class GoroneSystemExDetectionResult
    {
        public readonly List<UnityEngine.Object> ExMarkers = new();
        public readonly List<UnityEngine.Object> SupineMarkers = new();
        public readonly List<GoroneSystemExParameterHandle> VrcSupineParameters = new();

        public bool HasGoroneSystemEx => ExMarkers.Count > 0;
        public bool HasSupineSystem => HasGoroneSystemEx || SupineMarkers.Count > 0 || VrcSupineParameters.Count > 0;
    }

    internal sealed class GoroneSystemExParameterHandle
    {
        public ModularAvatarParameters Component;
        public ParameterConfig Config;
    }
}
