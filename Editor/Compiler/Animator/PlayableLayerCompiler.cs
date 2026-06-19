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
    internal static class PlayableLayerCompiler
    {
        public static void AddActionWeightBehavior(AnimatorState state, float goalWeight)
        {
            var behavior = state.AddStateMachineBehaviour<VRCPlayableLayerControl>();
            behavior.layer = VRC_PlayableLayerControl.BlendableLayer.Action;
            behavior.goalWeight = goalWeight;
            behavior.blendDuration = 0f;
            behavior.debugString = goalWeight > 0.5f
                ? "PoseTune Enable Action Layer"
                : "PoseTune Disable Action Layer";
        }
    }
}
