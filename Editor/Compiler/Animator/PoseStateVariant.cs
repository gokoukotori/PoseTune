using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseStateVariants
    {
        public AnimatorState BaseState { get; set; }
        public AnimatorState DesktopLowerBodyState { get; set; }
        public AnimatorState VrState { get; set; }
        public AnimatorState FullBodyState { get; set; }
        public int BaseTrackingVoteId { get; set; }
        public int DesktopLowerBodyTrackingVoteId { get; set; }
        public int VrTrackingVoteId { get; set; }
        public int FullBodyTrackingVoteId { get; set; }
        public TrackingPolicyData BaseTrackingPolicy { get; set; }
        public TrackingPolicyData DesktopLowerBodyTrackingPolicy { get; set; }
        public TrackingPolicyData VrTrackingPolicy { get; set; }
        public TrackingPolicyData FullBodyTrackingPolicy { get; set; }
        public AnimatorState BaseHandoff { get; set; }
        public AnimatorState DesktopLowerBodyHandoff { get; set; }
        public AnimatorState VrHandoff { get; set; }
        public AnimatorState FullBodyHandoff { get; set; }
        public bool NeedsPoseSpaceVrVariant { get; set; }
        public bool NeedsDesktopLowerBodyLockVariant { get; set; }
    }
}
