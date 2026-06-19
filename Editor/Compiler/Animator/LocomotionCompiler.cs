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
    internal static class LocomotionCompiler
    {
        public static void AddLocomotionBehavior(AnimatorState state, bool disableLocomotion)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAnimatorLocomotionControl>();
            behavior.disableLocomotion = disableLocomotion;
            behavior.debugString = disableLocomotion ? "PoseTune Disable Locomotion" : "PoseTune Restore Locomotion";
        }
    }
}
