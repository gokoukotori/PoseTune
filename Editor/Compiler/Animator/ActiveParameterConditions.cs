using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    /// <summary>
    /// Conditions on the generated group-active Float parameters, which state drivers set to 0 or 1.
    /// </summary>
    internal static class ActiveParameterConditions
    {
        public const float Threshold = 0.5f;

        public static void AddActiveCondition(AnimatorStateTransition transition, string parameter)
        {
            transition.AddCondition(AnimatorConditionMode.Greater, Threshold, parameter);
        }

        public static void AddInactiveCondition(AnimatorStateTransition transition, string parameter)
        {
            transition.AddCondition(AnimatorConditionMode.Less, Threshold, parameter);
        }
    }
}
