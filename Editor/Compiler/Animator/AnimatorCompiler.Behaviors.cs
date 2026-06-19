using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void AddFbtEntryCondition(
            AnimatorStateTransition transition,
            PoseTuneRoot root,
            PoseDefinition pose,
            bool fullBodyVariant)
        {
            TrackingGuardCompiler.AddEntryConditions(
                transition,
                TrackingGuardCompiler.PoseEntryProfile(root, pose, fullBodyVariant));
        }
    }
}
