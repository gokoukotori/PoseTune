using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class AnimatorCompiler
    {
        private static bool AddPoseEntryTransitions(
            AnimatorControllerLayer layer,
            PoseGraph graph,
            PoseGroupDefinition group,
            PoseDefinition pose,
            PoseDefinition autoPose,
            PoseStateVariants variants,
            HashSet<string> duplicateStateBaseNames,
            List<PoseGroupDefinition> exclusiveResetTargets,
            List<string> poseActiveParameters,
            bool controlsActionPlayable,
            string activeParameter,
            string poseActiveParameter,
            int x,
            int y)
        {
            var state = variants.BaseState;
            var desktopLowerBodyState = variants.DesktopLowerBodyState;
            var vrState = variants.VrState;
            var fbtState = variants.FullBodyState;
            var needsPoseSpaceVrVariant = variants.NeedsPoseSpaceVrVariant;
            var needsDesktopLowerBodyLockVariant = variants.NeedsDesktopLowerBodyLockVariant;

            var conditionBranches = pose.ConditionBranches.Count > 0
                ? pose.ConditionBranches
                : new List<List<ParameterConditionData>> { pose.Conditions };
            var hasAutoEntry = conditionBranches.Any(branch =>
                AllowsAutoEntry(graph.RootComponent, group, pose, autoPose, branch));
            AnimatorState commit = null;
            AnimatorState desktopCommit = null;
            AnimatorState vrCommit = null;
            AnimatorState fbtCommit = null;
            foreach (var branch in conditionBranches)
            {
                if (AllowsManualEntry(graph.RootComponent, group))
                {
                    var manualTarget = state;
                    if (group.Exclusive && exclusiveResetTargets.Count > 0)
                    {
                        commit ??= CreateExclusiveCommitState(layer, group, pose, state,
                            duplicateStateBaseNames,
                            exclusiveResetTargets, poseActiveParameters, controlsActionPlayable,
                            activeParameter, true, x + 280, y,
                            trackingContextId: variants.BaseTrackingContextId);
                        manualTarget = commit;
                    }

                    var enter = layer.stateMachine.AddAnyStateTransition(manualTarget);
                    enter.hasExitTime = false;
                    enter.duration = 0f;
                    enter.canTransitionToSelf = false;
                    AddManualModeEntryCondition(enter, graph.RootComponent);
                    enter.AddCondition(AnimatorConditionMode.Equals, pose.SelectionValue(graph.RootComponent), group.ParameterName);
                    AddManualCommitReentryGuard(enter, manualTarget, state, poseActiveParameter);
                    AddFbtEntryCondition(enter, graph.RootComponent, pose, false);
                    if (needsDesktopLowerBodyLockVariant)
                    {
                        AddVrModeCondition(enter);
                    }
                    else
                    {
                        AddPoseSpaceScopeCondition(enter, pose);
                    }
                    AddConditions(enter, branch);

                    if (desktopLowerBodyState != null)
                    {
                        var desktopTarget = desktopLowerBodyState;
                        if (group.Exclusive && exclusiveResetTargets.Count > 0)
                        {
                            desktopCommit ??= CreateExclusiveCommitState(layer, group, pose, desktopLowerBodyState,
                                duplicateStateBaseNames,
                                exclusiveResetTargets, poseActiveParameters, controlsActionPlayable,
                                activeParameter, true, x + 420, y,
                                PoseStateVariantRules.DesktopLowerBodyTrackingPolicy(pose.TrackingPolicy), "_Desktop",
                                variants.DesktopLowerBodyTrackingContextId);
                            desktopTarget = desktopCommit;
                        }

                        var desktopEnter = layer.stateMachine.AddAnyStateTransition(desktopTarget);
                        desktopEnter.hasExitTime = false;
                        desktopEnter.duration = 0f;
                        desktopEnter.canTransitionToSelf = false;
                        AddManualModeEntryCondition(desktopEnter, graph.RootComponent);
                        desktopEnter.AddCondition(AnimatorConditionMode.Equals, pose.SelectionValue(graph.RootComponent), group.ParameterName);
                        AddManualCommitReentryGuard(desktopEnter, desktopTarget, desktopLowerBodyState, poseActiveParameter);
                        AddFbtEntryCondition(desktopEnter, graph.RootComponent, pose, false);
                        AddDesktopModeCondition(desktopEnter);
                        AddConditions(desktopEnter, branch);
                    }

                    if (vrState != null)
                    {
                        var vrTarget = vrState;
                        if (group.Exclusive && exclusiveResetTargets.Count > 0)
                        {
                            vrCommit ??= CreateExclusiveCommitState(layer, group, pose, vrState,
                                duplicateStateBaseNames,
                                exclusiveResetTargets, poseActiveParameters, controlsActionPlayable,
                                activeParameter, false, x + 420, y,
                                trackingContextId: variants.VrTrackingContextId);
                            vrTarget = vrCommit;
                        }

                        var vrEnter = layer.stateMachine.AddAnyStateTransition(vrTarget);
                        vrEnter.hasExitTime = false;
                        vrEnter.duration = 0f;
                        vrEnter.canTransitionToSelf = false;
                        AddManualModeEntryCondition(vrEnter, graph.RootComponent);
                        vrEnter.AddCondition(AnimatorConditionMode.Equals, pose.SelectionValue(graph.RootComponent), group.ParameterName);
                        AddManualCommitReentryGuard(vrEnter, vrTarget, vrState, poseActiveParameter);
                        AddFbtEntryCondition(vrEnter, graph.RootComponent, pose, false);
                        AddPoseSpaceScopeCondition(vrEnter, pose, true);
                        AddConditions(vrEnter, branch);
                    }

                    if (fbtState != null)
                    {
                        var fbtTarget = fbtState;
                        if (group.Exclusive && exclusiveResetTargets.Count > 0)
                        {
                            fbtCommit ??= CreateExclusiveCommitState(layer, group, pose, fbtState,
                                duplicateStateBaseNames,
                                exclusiveResetTargets, poseActiveParameters, controlsActionPlayable,
                                activeParameter, !needsPoseSpaceVrVariant, x + 560, y,
                                pose.FullBodyTrackingPolicy, "_FBT",
                                variants.FullBodyTrackingContextId);
                            fbtTarget = fbtCommit;
                        }

                        var fbtEnter = layer.stateMachine.AddAnyStateTransition(fbtTarget);
                        fbtEnter.hasExitTime = false;
                        fbtEnter.duration = 0f;
                        fbtEnter.canTransitionToSelf = false;
                        AddManualModeEntryCondition(fbtEnter, graph.RootComponent);
                        fbtEnter.AddCondition(AnimatorConditionMode.Equals, pose.SelectionValue(graph.RootComponent), group.ParameterName);
                        AddManualCommitReentryGuard(fbtEnter, fbtTarget, fbtState, poseActiveParameter);
                        AddFbtEntryCondition(fbtEnter, graph.RootComponent, pose, true);
                        AddPoseSpaceScopeCondition(fbtEnter, pose, needsPoseSpaceVrVariant);
                        AddConditions(fbtEnter, branch);
                    }
                }

                if (AllowsAutoEntry(graph.RootComponent, group, pose, autoPose, branch))
                {
                    var enter = layer.stateMachine.AddAnyStateTransition(state);
                    enter.hasExitTime = false;
                    enter.duration = 0f;
                    enter.canTransitionToSelf = false;
                    enter.AddCondition(AnimatorConditionMode.Equals, 1, graph.RootComponent.Parameter(PoseTuneNames.Mode));
                    AddSelectedPoseAutoEntryCondition(enter, graph.RootComponent, group, pose);
                    AddFbtEntryCondition(enter, graph.RootComponent, pose, false);
                    if (needsDesktopLowerBodyLockVariant)
                    {
                        AddVrModeCondition(enter);
                    }
                    else
                    {
                        AddPoseSpaceScopeCondition(enter, pose);
                    }
                    AddConditions(enter, AutoContextConditions(graph.RootComponent, group));
                    AddConditions(enter, branch);

                    if (desktopLowerBodyState != null)
                    {
                        var desktopEnter = layer.stateMachine.AddAnyStateTransition(desktopLowerBodyState);
                        desktopEnter.hasExitTime = false;
                        desktopEnter.duration = 0f;
                        desktopEnter.canTransitionToSelf = false;
                        desktopEnter.AddCondition(AnimatorConditionMode.Equals, 1,
                            graph.RootComponent.Parameter(PoseTuneNames.Mode));
                        AddSelectedPoseAutoEntryCondition(desktopEnter, graph.RootComponent, group, pose);
                        AddFbtEntryCondition(desktopEnter, graph.RootComponent, pose, false);
                        AddDesktopModeCondition(desktopEnter);
                        AddConditions(desktopEnter, AutoContextConditions(graph.RootComponent, group));
                        AddConditions(desktopEnter, branch);
                    }

                    if (vrState != null)
                    {
                        var vrEnter = layer.stateMachine.AddAnyStateTransition(vrState);
                        vrEnter.hasExitTime = false;
                        vrEnter.duration = 0f;
                        vrEnter.canTransitionToSelf = false;
                        vrEnter.AddCondition(AnimatorConditionMode.Equals, 1,
                            graph.RootComponent.Parameter(PoseTuneNames.Mode));
                        AddSelectedPoseAutoEntryCondition(vrEnter, graph.RootComponent, group, pose);
                        AddFbtEntryCondition(vrEnter, graph.RootComponent, pose, false);
                        AddPoseSpaceScopeCondition(vrEnter, pose, true);
                        AddConditions(vrEnter, AutoContextConditions(graph.RootComponent, group));
                        AddConditions(vrEnter, branch);
                    }

                    if (fbtState != null)
                    {
                        var fbtEnter = layer.stateMachine.AddAnyStateTransition(fbtState);
                        fbtEnter.hasExitTime = false;
                        fbtEnter.duration = 0f;
                        fbtEnter.canTransitionToSelf = false;
                        fbtEnter.AddCondition(AnimatorConditionMode.Equals, 1,
                            graph.RootComponent.Parameter(PoseTuneNames.Mode));
                        AddSelectedPoseAutoEntryCondition(fbtEnter, graph.RootComponent, group, pose);
                        AddFbtEntryCondition(fbtEnter, graph.RootComponent, pose, true);
                        AddPoseSpaceScopeCondition(fbtEnter, pose, needsPoseSpaceVrVariant);
                        AddConditions(fbtEnter, AutoContextConditions(graph.RootComponent, group));
                        AddConditions(fbtEnter, branch);
                    }
                }
            }

            return hasAutoEntry;
        }
    }
}
