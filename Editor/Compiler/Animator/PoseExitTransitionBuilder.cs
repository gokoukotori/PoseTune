using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static void AddPoseExitTransitions(
            PoseStateVariants variants,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseSelectionPlan poseSelection,
            bool hasAutoEntry)
        {
            AddPoseExitTransitions(variants.BaseState, variants.BaseHandoff, graph, group, pose, poseSelection, hasAutoEntry, false);
            if (variants.NeedsDesktopLowerBodyLockVariant)
            {
                AddVrModeInvalidExitTransition(variants.BaseState, variants.BaseHandoff);
            }
            else if (variants.NeedsPoseSpaceVrVariant)
            {
                AddDesktopModeInvalidExitTransition(variants.BaseState, variants.BaseHandoff);
            }

            if (variants.FullBodyState != null)
            {
                AddPoseExitTransitions(variants.FullBodyState, variants.FullBodyHandoff, graph, group, pose, poseSelection, hasAutoEntry, true);
            }

            if (variants.DesktopLowerBodyState != null)
            {
                AddPoseExitTransitions(variants.DesktopLowerBodyState, variants.DesktopLowerBodyHandoff, graph, group, pose, poseSelection, hasAutoEntry, false);
                AddDesktopModeInvalidExitTransition(variants.DesktopLowerBodyState, variants.DesktopLowerBodyHandoff);
            }

            if (variants.VrState != null)
            {
                AddPoseExitTransitions(variants.VrState, variants.VrHandoff, graph, group, pose, poseSelection, hasAutoEntry, false);
                if (variants.NeedsPoseSpaceVrVariant)
                {
                    AddVrModeInvalidExitTransition(variants.VrState, variants.VrHandoff);
                }
            }
        }

        private static void AddPoseExitTransitions(
            AnimatorState state,
            AnimatorState cleanup,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseSelectionPlan poseSelection,
            bool hasAutoEntry,
            bool fullBodyVariant)
        {
            if (AllowsManualEntry(graph.RootComponent, group))
            {
                var toResetByGroup = state.AddTransition(cleanup);
                toResetByGroup.hasExitTime = false;
                toResetByGroup.duration = 0f;
                AddManualModeEntryCondition(toResetByGroup, graph.RootComponent);
                AddManualGroupDeselectedCondition(toResetByGroup, poseSelection, pose);
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
                AddSelectedPoseAutoExitTransition(
                    state,
                    cleanup,
                    graph.RootComponent,
                    group,
                    pose,
                    poseSelection);
            }
        }
    }
}
