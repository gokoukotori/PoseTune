using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    /// <summary>
    /// Other playable layers reset what PoseTune applied on state entry. The VRChat default locomotion
    /// layer writes VRCAnimatorTrackingControl in its jump, fall and landing states. The default Action
    /// layer's emote and AFK BlendOut states set the Action weight to 0 and tracking back to Tracking.
    /// Either leaves the pose broken or hidden until it is selected again.
    /// This layer raises <see cref="PoseTuneNames.PoseRefreshParameter"/> while one of those disturbances
    /// can happen and for a bounded settle time after it; held states re-enter themselves while it is raised.
    /// </summary>
    internal static class PoseRefreshLayerCompiler
    {
        internal const float RefreshIntervalSeconds = 0.1f;
        internal const float LandingSettleSeconds = 2f;
        internal const float AfkSettleSeconds = 2f;

        // Emote buttons reset VRCEmote after about a second while one-shot emotes keep playing (up to about
        // 10 seconds) before the default Action layer's BlendOut runs.
        internal const float EmoteSettleSeconds = 12f;

        private const float RefreshExitTime = 0.95f;

        public static bool IsNeeded(PoseGraph graph)
        {
            return ParameterAllocator.NeedsTrackingArbiter(graph) ||
                   PoseTuneCompilerRules.ControlsActionPlayable(graph?.RootComponent);
        }

        public static void Compile(AnimatorBuildResult result)
        {
            var layer = AnimatorLayerFactory.NewLayer(PoseTuneNames.PoseRefreshLayerName);
            var empty = AnimatorLayerFactory.EmptyClip(layer.name + "_Empty");
            result.GeneratedAssets.Add(empty);

            var idle = AddState(layer, "Idle", new Vector3(240, 80), empty, false);
            layer.stateMachine.defaultState = idle;

            var airborne = AddState(layer, "Airborne", new Vector3(520, -40), empty, true);
            var landingSettle = AddSettleState(result, layer, "LandingSettle", new Vector3(800, -40),
                LandingSettleSeconds);
            var emote = AddState(layer, "Emote", new Vector3(520, 80), empty, true);
            var emoteSettle = AddSettleState(result, layer, "EmoteSettle", new Vector3(800, 80),
                EmoteSettleSeconds);
            var afk = AddState(layer, "Afk", new Vector3(520, 200), empty, true);
            var afkSettle = AddSettleState(result, layer, "AfkSettle", new Vector3(800, 200), AfkSettleSeconds);

            // From idle any disturbance raises the refresh.
            AddAfkStart(idle, afk);
            AddEmoteStart(idle, emote);
            AddTakeOff(idle, airborne);

            // Jump: settle after landing. An emote or AFK starting meanwhile takes over with its own window.
            AddAfkStart(airborne, afk);
            AddEmoteStart(airborne, emote);
            AddImmediate(airborne, landingSettle, AnimatorConditionMode.If, "Grounded");
            AddImmediate(airborne, landingSettle, AnimatorConditionMode.If, "Seated");
            AddAfkStart(landingSettle, afk);
            AddEmoteStart(landingSettle, emote);
            AddTakeOff(landingSettle, airborne);
            AnimatorTransitionFactory.AfterExitTime(landingSettle, idle);

            // Emote: the settle window covers one-shot emotes that outlive the button. A jump inside it does
            // not shorten it; the refresh stays raised and the jump is picked up again from idle if needed.
            var emoteEnd = AnimatorTransitionFactory.Immediate(emote, emoteSettle);
            emoteEnd.AddCondition(AnimatorConditionMode.Equals, 0f, "VRCEmote");
            AddEmoteStart(emoteSettle, emote);
            AddAfkStart(emoteSettle, afk);
            AnimatorTransitionFactory.AfterExitTime(emoteSettle, idle);

            // AFK: the default Action layer blends out as soon as AFK clears.
            AddImmediate(afk, afkSettle, AnimatorConditionMode.IfNot, "AFK");
            AddAfkStart(afkSettle, afk);
            AddEmoteStart(afkSettle, emote);
            AddTakeOff(afkSettle, airborne);
            AnimatorTransitionFactory.AfterExitTime(afkSettle, idle);

            result.TargetController.AddLayer(layer);
        }

        public static AnimationClip CreateRefreshLoopClip(string name)
        {
            var clip = AnimatorLayerFactory.ResetHoldClip(name, RefreshIntervalSeconds);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        /// <summary>
        /// Must be called after the state's other transitions so that winner changes, lock releases and
        /// exits keep priority over the refresh loop.
        /// </summary>
        public static void AddRefreshLoop(AnimatorState held, AnimationClip refreshLoopClip)
        {
            held.motion = refreshLoopClip;
            var refresh = AnimatorTransitionFactory.AfterExitTime(held, held, RefreshExitTime);
            refresh.AddCondition(AnimatorConditionMode.If, 0f, PoseTuneNames.PoseRefreshParameter);
        }

        private static AnimatorState AddState(
            AnimatorControllerLayer layer,
            string name,
            Vector3 position,
            Motion motion,
            bool raisesRefresh)
        {
            var state = AnimatorLayerFactory.AddState(layer.stateMachine, name, position, motion);
            ParameterDriverCompiler.SetPoseRefresh(state, raisesRefresh);
            return state;
        }

        private static AnimatorState AddSettleState(
            AnimatorBuildResult result,
            AnimatorControllerLayer layer,
            string name,
            Vector3 position,
            float seconds)
        {
            var hold = AnimatorLayerFactory.ResetHoldClip(layer.name + "_" + name, seconds);
            result.GeneratedAssets.Add(hold);
            return AnimatorLayerFactory.AddState(layer.stateMachine, name, position, hold);
        }

        private static void AddTakeOff(AnimatorState from, AnimatorState airborne)
        {
            var takeOff = AnimatorTransitionFactory.Immediate(from, airborne);
            takeOff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            takeOff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Seated");
        }

        private static void AddEmoteStart(AnimatorState from, AnimatorState emote)
        {
            var start = AnimatorTransitionFactory.Immediate(from, emote);
            start.AddCondition(AnimatorConditionMode.NotEqual, 0f, "VRCEmote");
        }

        private static void AddAfkStart(AnimatorState from, AnimatorState afk)
        {
            AddImmediate(from, afk, AnimatorConditionMode.If, "AFK");
        }

        private static void AddImmediate(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            string parameter)
        {
            var transition = AnimatorTransitionFactory.Immediate(from, to);
            transition.AddCondition(mode, 0f, parameter);
        }
    }
}
