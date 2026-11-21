using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorTransitionFactory
    {
        /// <summary>
        /// Any State transition that fires as soon as its conditions hold and never re-enters its destination.
        /// Any State transitions are evaluated in creation order, so call order is priority.
        /// </summary>
        public static AnimatorStateTransition ImmediateFromAny(AnimatorStateMachine stateMachine, AnimatorState to)
        {
            var transition = stateMachine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            return transition;
        }

        /// <summary>
        /// State transition that fires as soon as its conditions hold.
        /// </summary>
        public static AnimatorStateTransition Immediate(AnimatorState from, AnimatorState to)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            return transition;
        }

        /// <summary>
        /// State transition that fires once <paramref name="from"/> reaches <paramref name="exitTime"/>.
        /// </summary>
        public static AnimatorStateTransition AfterExitTime(
            AnimatorState from,
            AnimatorState to,
            float exitTime = 1f,
            float duration = 0f)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = duration;
            return transition;
        }
    }
}
