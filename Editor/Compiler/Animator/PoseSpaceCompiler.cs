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
    internal static class PoseSpaceCompiler
    {
        public static void AddEnterPoseSpaceBehavior(AnimatorState state, PoseSpacePolicy policy)
        {
            if (policy == null || !policy.enabled)
            {
                return;
            }

            var behavior = state.AddStateMachineBehaviour<VRCAnimatorTemporaryPoseSpace>();
            behavior.enterPoseSpace = policy.enterPoseSpace;
            behavior.fixedDelay = policy.fixedDelay;
            behavior.delayTime = policy.delayTime;
        }

        public static void AddExitPoseSpaceBehavior(AnimatorState state)
        {
            var behavior = state.AddStateMachineBehaviour<VRCAnimatorTemporaryPoseSpace>();
            behavior.enterPoseSpace = false;
        }
    }
}
