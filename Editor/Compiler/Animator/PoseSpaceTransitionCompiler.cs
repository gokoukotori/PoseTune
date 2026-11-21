using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseSpaceTransitionCompiler
    {
        internal static void AddPoseSpaceScopeCondition(
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
                ModeTransitionCompiler.AddVrModeCondition(transition);
                return;
            }

            switch (pose.PoseSpace.scope)
            {
                case PoseSpaceScope.DesktopOnly:
                    ModeTransitionCompiler.AddDesktopModeCondition(transition);
                    break;
                case PoseSpaceScope.VROnly:
                    ModeTransitionCompiler.AddVrModeCondition(transition);
                    break;
            }
        }
    }
}
