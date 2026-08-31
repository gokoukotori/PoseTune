using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ParameterDriverCompiler
    {
        public static void SetGroupActive(AnimatorState state, string parameterName, float value)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = value > 0.5f
                ? "PoseTune Mark Pose Active"
                : "PoseTune Mark Pose Inactive";
            behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = parameterName,
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = value
            });
        }

        public static void ResetExclusiveParameters(
            AnimatorState state,
            IEnumerable<string> parameterNames,
            bool localOnly)
        {
            var targets = parameterNames?
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter))
                .Distinct()
                .ToList() ?? new List<string>();
            if (targets.Count == 0)
            {
                return;
            }

            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = localOnly;
            behavior.debugString = "PoseTune Commit Exclusive Pose";
            foreach (var parameterName in targets)
            {
                behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = parameterName,
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = 0f
                });
            }
        }

        public static void SetPoseActive(AnimatorState state, string parameterName, float value)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = value > 0.5f
                ? "PoseTune Mark Pose Active"
                : "PoseTune Mark Pose Inactive";
            behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = parameterName,
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = value
            });
        }

        public static void SetTrackingVote(AnimatorState state, PoseGroupDefinition group, int voteId)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = voteId > 0
                ? "PoseTune Set Tracking Vote"
                : "PoseTune Clear Tracking Vote";
            behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = PoseTuneNames.TrackingVoteParameter(group),
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = voteId
            });
        }

        public static void RequestTrackingReset(AnimatorState state, IEnumerable<TrackingPart> parts)
        {
            var targets = parts?.Distinct().ToList() ?? new List<TrackingPart>();
            if (targets.Count == 0)
            {
                return;
            }

            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = "PoseTune Request Tracking Reset";
            foreach (var part in targets)
            {
                behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = PoseTuneNames.TrackingResetParameter(part),
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = 1f
                });
            }
        }

        public static void ClearTrackingReset(AnimatorState state, TrackingPart part)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = "PoseTune Clear Tracking Reset";
            behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = PoseTuneNames.TrackingResetParameter(part),
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = 0f
            });
        }

        public static void ResetPoseActiveParameters(AnimatorState state, IEnumerable<string> parameterNames)
        {
            var targets = parameterNames?
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter))
                .Distinct()
                .ToList() ?? new List<string>();
            if (targets.Count == 0)
            {
                return;
            }

            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = false;
            behavior.debugString = "PoseTune Reset Pose Active";
            foreach (var parameterName in targets)
            {
                behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = parameterName,
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = 0f
                });
            }
        }
    }
}
