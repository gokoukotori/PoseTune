using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed partial class PoseTuneAnimatorValidator
    {
        private static string NormalizeInternalParameterName(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                !name.StartsWith(PoseTuneNames.InternalParameterPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            var localizationSuffix = name.IndexOf('$');
            return localizationSuffix < 0 ? name : name.Substring(0, localizationSuffix);
        }

        private static AnimatorControllerParameter ParameterView(
            string name,
            AnimatorControllerParameter parameter)
        {
            return new AnimatorControllerParameter
            {
                name = NormalizeInternalParameterName(name),
                type = parameter.type,
                defaultBool = parameter.defaultBool,
                defaultFloat = parameter.defaultFloat,
                defaultInt = parameter.defaultInt
            };
        }

        private static AnimatorCondition[] ConditionViews(IEnumerable<AnimatorCondition> conditions)
        {
            return (conditions ?? Enumerable.Empty<AnimatorCondition>())
                .Select(condition => new AnimatorCondition
                {
                    parameter = NormalizeInternalParameterName(condition.parameter),
                    mode = condition.mode,
                    threshold = condition.threshold
                })
                .ToArray();
        }

        // NDMF keeps playable layers virtualized while Transforming passes are running.  The
        // validator intentionally reads through this small immutable view so the generated
        // controller and the post-MA virtual controller are checked by exactly the same rules.
        private sealed class ControllerView
        {
            public AnimatorControllerParameter[] parameters = Array.Empty<AnimatorControllerParameter>();
            public LayerView[] layers = Array.Empty<LayerView>();

            public static ControllerView Create(AnimatorController controller)
            {
                if (controller == null)
                {
                    return null;
                }

                return new ControllerView
                {
                    parameters = (controller.parameters ?? Array.Empty<AnimatorControllerParameter>())
                        .Where(parameter => parameter != null)
                        .Select(parameter => ParameterView(parameter.name, parameter))
                        .ToArray(),
                    layers = (controller.layers ?? Array.Empty<AnimatorControllerLayer>())
                        .Where(layer => layer != null)
                        .Select(LayerView.Create)
                        .ToArray()
                };
            }

            public static ControllerView Create(VirtualAnimatorController controller)
            {
                if (controller == null)
                {
                    return null;
                }

                return new ControllerView
                {
                    parameters = controller.Parameters
                        .Select(parameter => ParameterView(parameter.Key, parameter.Value))
                        .ToArray(),
                    layers = controller.Layers
                        .Where(layer => layer != null)
                        .Select(LayerView.Create)
                        .ToArray()
                };
            }
        }

        private sealed class LayerView
        {
            public string name;
            public StateMachineView stateMachine;

            public static LayerView Create(AnimatorControllerLayer layer)
            {
                return new LayerView
                {
                    name = layer.name,
                    stateMachine = StateMachineView.Create(layer.stateMachine)
                };
            }

            public static LayerView Create(VirtualLayer layer)
            {
                return new LayerView
                {
                    name = layer.Name,
                    stateMachine = StateMachineView.Create(layer.StateMachine)
                };
            }
        }

        private sealed class StateMachineView
        {
            public StateView[] states = Array.Empty<StateView>();
            public TransitionView[] anyStateTransitions = Array.Empty<TransitionView>();
            private Dictionary<string, StateView> statesByName;

            public StateView FindState(string stateName)
            {
                // The view is immutable for this validation; preserve first-match semantics.
                statesByName ??= states.Where(state => state != null && state.name != null)
                    .GroupBy(state => state.name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                if (stateName == null)
                    return states.FirstOrDefault(state => state != null && state.name == null);
                return statesByName.TryGetValue(stateName, out var state) ? state : null;
            }

            public static StateMachineView Create(AnimatorStateMachine stateMachine)
            {
                if (stateMachine == null)
                {
                    return null;
                }

                var sourceStates = (stateMachine.states ?? Array.Empty<ChildAnimatorState>())
                    .Select(child => child.state)
                    .Where(state => state != null)
                    .ToList();
                var views = sourceStates.ToDictionary(state => state, StateView.Create);
                StateView Resolve(AnimatorState state)
                {
                    if (state == null)
                    {
                        return null;
                    }

                    if (!views.TryGetValue(state, out var view))
                    {
                        view = StateView.Create(state);
                        views.Add(state, view);
                    }

                    return view;
                }

                foreach (var state in sourceStates)
                {
                    views[state].transitions = (state.transitions ?? Array.Empty<AnimatorStateTransition>())
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray();
                }

                return new StateMachineView
                {
                    states = sourceStates.Select(Resolve).ToArray(),
                    anyStateTransitions = (stateMachine.anyStateTransitions ??
                                           Array.Empty<AnimatorStateTransition>())
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray()
                };
            }

            public static StateMachineView Create(VirtualStateMachine stateMachine)
            {
                if (stateMachine == null)
                {
                    return null;
                }

                var sourceStates = stateMachine.States
                    .Select(child => child.State)
                    .Where(state => state != null)
                    .ToList();
                var views = sourceStates.ToDictionary(state => state, StateView.Create);
                StateView Resolve(VirtualState state)
                {
                    if (state == null)
                    {
                        return null;
                    }

                    if (!views.TryGetValue(state, out var view))
                    {
                        view = StateView.Create(state);
                        views.Add(state, view);
                    }

                    return view;
                }

                foreach (var state in sourceStates)
                {
                    views[state].transitions = state.Transitions
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray();
                }

                return new StateMachineView
                {
                    states = sourceStates.Select(Resolve).ToArray(),
                    anyStateTransitions = stateMachine.AnyStateTransitions
                        .Where(transition => transition != null)
                        .Select(transition => TransitionView.Create(transition, Resolve))
                        .ToArray()
                };
            }
        }

        private sealed class StateView
        {
            public string name;
            public StateMachineBehaviour[] behaviours = Array.Empty<StateMachineBehaviour>();
            public ParameterWriteView[] parameterWrites = Array.Empty<ParameterWriteView>();
            public TransitionView[] transitions = Array.Empty<TransitionView>();

            public static StateView Create(AnimatorState state)
            {
                return new StateView
                {
                    name = state.name,
                    behaviours = state.behaviours ?? Array.Empty<StateMachineBehaviour>(),
                    parameterWrites = ParameterWriteView.Create(
                        state.behaviours ?? Array.Empty<StateMachineBehaviour>())
                };
            }

            public static StateView Create(VirtualState state)
            {
                return new StateView
                {
                    name = state.Name,
                    behaviours = state.Behaviours.ToArray(),
                    parameterWrites = ParameterWriteView.Create(state.Behaviours)
                };
            }
        }

        private sealed class ParameterWriteView
        {
            public string name;
            public float value;

            public static ParameterWriteView[] Create(IEnumerable<StateMachineBehaviour> behaviours)
            {
                return (behaviours ?? Enumerable.Empty<StateMachineBehaviour>())
                    .OfType<VRCAvatarParameterDriver>()
                    .SelectMany(driver =>
                        driver.parameters ?? new List<VRC_AvatarParameterDriver.Parameter>())
                    .Where(parameter => parameter != null &&
                                        parameter.type == VRC_AvatarParameterDriver.ChangeType.Set)
                    .Select(parameter => new ParameterWriteView
                    {
                        name = NormalizeInternalParameterName(parameter.name),
                        value = parameter.value
                    })
                    .ToArray();
            }
        }

        private sealed class TransitionView
        {
            public StateView destinationState;
            public AnimatorCondition[] conditions = Array.Empty<AnimatorCondition>();
            public bool canTransitionToSelf;

            public static TransitionView Create(
                AnimatorStateTransition transition,
                Func<AnimatorState, StateView> resolve)
            {
                return new TransitionView
                {
                    destinationState = resolve(transition.destinationState),
                    conditions = ConditionViews(transition.conditions),
                    canTransitionToSelf = transition.canTransitionToSelf
                };
            }

            public static TransitionView Create(
                VirtualStateTransition transition,
                Func<VirtualState, StateView> resolve)
            {
                return new TransitionView
                {
                    destinationState = resolve(transition.DestinationState),
                    conditions = ConditionViews(transition.Conditions),
                    canTransitionToSelf = transition.CanTransitionToSelf
                };
            }
        }
    }
}
