using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void AddPoseSpaceScopeCondition(
            AnimatorStateTransition transition,
            PoseDefinition pose,
            bool vrNormalVariant = false)
        {
            if (pose?.PoseSpace == null || !pose.PoseSpace.enabled)
            {
                return;
            }

            if (PoseStateVariantRules.NeedsPoseSpaceVrVariant(pose) && vrNormalVariant)
            {
                transition.AddCondition(AnimatorConditionMode.Greater, 0f, "VRMode");
                return;
            }

            switch (pose.PoseSpace.scope)
            {
                case PoseSpaceScope.DesktopOnly:
                    transition.AddCondition(AnimatorConditionMode.Less, 1f, "VRMode");
                    break;
                case PoseSpaceScope.VROnly:
                    transition.AddCondition(AnimatorConditionMode.Greater, 0f, "VRMode");
                    break;
            }
        }
    }
}
