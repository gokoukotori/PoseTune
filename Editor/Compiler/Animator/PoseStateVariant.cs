using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseStateVariants
    {
        public AnimatorState BaseState { get; set; }
        public AnimatorState DesktopLowerBodyState { get; set; }
        public AnimatorState VrState { get; set; }
        public AnimatorState FullBodyState { get; set; }
        public bool NeedsPoseSpaceVrVariant { get; set; }
        public bool NeedsDesktopLowerBodyLockVariant { get; set; }
    }
}
