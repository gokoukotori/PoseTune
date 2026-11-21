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
            AddSetDriver(
                state,
                false,
                value > 0.5f
                    ? "PoseTune Mark Pose Active"
                    : "PoseTune Mark Pose Inactive",
                new[] { (parameterName, value) });
        }

        public static void ResetExclusiveParameters(
            AnimatorState state,
            IEnumerable<string> parameterNames,
            bool localOnly)
        {
            var targets = NonBlankDistinct(parameterNames);
            if (targets.Count == 0)
            {
                return;
            }

            AddSetDriver(
                state,
                localOnly,
                PoseTuneNames.CommitExclusivePoseDebugString,
                targets.Select(parameterName => (parameterName, 0f)));
        }

        public static void SetPoseActive(AnimatorState state, string parameterName, float value)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            SetGroupActive(state, parameterName, value);
        }

        public static void SetTrackingVote(AnimatorState state, PoseGroupDefinition group, int voteId)
        {
            AddSetDriver(
                state,
                false,
                voteId > 0
                    ? "PoseTune Set Tracking Vote"
                    : "PoseTune Clear Tracking Vote",
                new[] { (PoseTuneNames.TrackingVoteParameter(group), (float)voteId) });
        }

        public static void RequestTrackingReset(AnimatorState state, IEnumerable<TrackingPart> parts)
        {
            var targets = parts?.Distinct().ToList() ?? new List<TrackingPart>();
            if (targets.Count == 0)
            {
                return;
            }

            AddSetDriver(
                state,
                false,
                "PoseTune Request Tracking Reset",
                targets.Select(part => (PoseTuneNames.TrackingResetParameter(part), 1f)));
        }

        public static void ClearTrackingReset(AnimatorState state, TrackingPart part)
        {
            AddSetDriver(
                state,
                false,
                "PoseTune Clear Tracking Reset",
                new[] { (PoseTuneNames.TrackingResetParameter(part), 0f) });
        }

        public static void ResetPoseActiveParameters(AnimatorState state, IEnumerable<string> parameterNames)
        {
            var targets = NonBlankDistinct(parameterNames);
            if (targets.Count == 0)
            {
                return;
            }

            AddSetDriver(
                state,
                false,
                "PoseTune Reset Pose Active",
                targets.Select(parameterName => (parameterName, 0f)));
        }

        public static void SetPoseRefresh(AnimatorState state, bool active)
        {
            // Every client runs this animator and the default Action BlendOut zeroes the Action weight on remote
            // copies too, so remote copies must refresh as well. Grounded, VRCEmote and AFK reach them.
            AddSetDriver(
                state,
                false,
                active
                    ? "PoseTune Start Pose Refresh"
                    : "PoseTune Stop Pose Refresh",
                new[] { (PoseTuneNames.PoseRefreshParameter, active ? 1f : 0f) });
        }

        private static List<string> NonBlankDistinct(IEnumerable<string> parameterNames)
        {
            return parameterNames?
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter))
                .Distinct()
                .ToList() ?? new List<string>();
        }

        private static void AddSetDriver(
            AnimatorState state,
            bool localOnly,
            string debugString,
            IEnumerable<(string name, float value)> parameters)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            behavior.localOnly = localOnly;
            behavior.debugString = debugString;
            foreach (var (name, value) in parameters)
            {
                behavior.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = name,
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    value = value
                });
            }
        }
    }
}
