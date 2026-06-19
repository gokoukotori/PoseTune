using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class AnimatorLayerFactory
    {
        public static AnimatorControllerLayer NewLayer(string name)
        {
            return new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine { name = name }
            };
        }

        public static AnimationClip EmptyClip(string name)
        {
            return new AnimationClip { name = name };
        }

        public static AnimationClip ResetHoldClip(string name, float holdSeconds)
        {
            var clip = new AnimationClip { name = name };
            clip.SetCurve(
                "__PoseTune_ResetHold",
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Constant(0f, holdSeconds, 0f));
            return clip;
        }
    }
}
