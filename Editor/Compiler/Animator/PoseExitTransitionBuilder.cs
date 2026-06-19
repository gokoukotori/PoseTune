using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void AddPoseExitTransitions(
            PoseStateVariants variants,
            AnimatorState cleanup,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            bool hasAutoEntry)
        {
            AddPoseExitTransitions(variants.BaseState, cleanup, graph, group, pose, hasAutoEntry, false);
            if (variants.NeedsDesktopLowerBodyLockVariant)
            {
                AddVrModeInvalidExitTransition(variants.BaseState, cleanup);
            }

            if (variants.FullBodyState != null)
            {
                AddPoseExitTransitions(variants.FullBodyState, cleanup, graph, group, pose, hasAutoEntry, true);
            }

            if (variants.DesktopLowerBodyState != null)
            {
                AddPoseExitTransitions(variants.DesktopLowerBodyState, cleanup, graph, group, pose, hasAutoEntry, false);
                AddDesktopModeInvalidExitTransition(variants.DesktopLowerBodyState, cleanup);
            }

            if (variants.VrState != null)
            {
                AddPoseExitTransitions(variants.VrState, cleanup, graph, group, pose, hasAutoEntry, false);
            }
        }

        private static void AddPoseExitTransitions(
            AnimatorState state,
            AnimatorState cleanup,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            bool hasAutoEntry,
            bool fullBodyVariant)
        {
            if (AllowsManualEntry(graph.RootComponent, group))
            {
                var toResetByGroup = state.AddTransition(cleanup);
                toResetByGroup.hasExitTime = false;
                toResetByGroup.duration = 0f;
                AddManualModeEntryCondition(toResetByGroup, graph.RootComponent);
                AddManualGroupDeselectedCondition(toResetByGroup, group);
            }

            var toResetByMode = state.AddTransition(cleanup);
            toResetByMode.hasExitTime = false;
            toResetByMode.duration = 0f;
            AddModeExitCondition(toResetByMode, graph.RootComponent, group);

            if (ShouldResetManualPoseOnAutoMode(graph.RootComponent, group, hasAutoEntry))
            {
                var toResetByAutoMode = state.AddTransition(cleanup);
                toResetByAutoMode.hasExitTime = false;
                toResetByAutoMode.duration = 0f;
                toResetByAutoMode.AddCondition(AnimatorConditionMode.Equals, 1,
                    graph.RootComponent.Parameter(PoseTuneNames.Mode));
            }

            TrackingGuardCompiler.AddInvalidExitTransitions(
                state,
                cleanup,
                TrackingGuardCompiler.PoseEntryProfile(graph.RootComponent, pose, fullBodyVariant));

            AddConditionExitTransitions(state, cleanup, pose);
            if (hasAutoEntry)
            {
                AddAutoContextExitTransitions(state, cleanup, graph.RootComponent, group);
                AddSelectedPoseAutoExitTransition(state, cleanup, graph.RootComponent, group, pose);
            }
        }
    }
}
