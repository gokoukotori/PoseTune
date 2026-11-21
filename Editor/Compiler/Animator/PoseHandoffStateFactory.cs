using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    /// <summary>
    /// Creates the handoff (cleanup) state a pose leaves through: where configured it clears the group's tracking
    /// vote, requests a tracking reset and exits pose space, then clears the active parameters.
    /// </summary>
    internal static class PoseHandoffStateFactory
    {
        internal static AnimatorState Create(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariant variant,
            int x,
            int y)
        {
            var group = context.Group;
            var state = AnimatorLayerFactory.AddState(
                context.Layer.stateMachine,
                PoseStateNaming.CleanupName(pose, context.DuplicateStateBaseNames, variant.Suffix),
                new Vector3(x + variant.HandoffX, y),
                context.HandoffHold);
            if (group.EmitTrackingControl && ParameterAllocator.RequiresTrackingVote(context.Graph, group))
            {
                ParameterDriverCompiler.SetTrackingVote(state, group, 0);
            }

            if (group.GenerateResetOnExit)
            {
                ParameterDriverCompiler.RequestTrackingReset(
                    state,
                    TrackingArbiterCompiler.ControlledParts(variant.Policy));
            }

            if (PoseStateVariantRules.EntersPoseSpace(pose, variant.AppliesPoseSpace))
            {
                PoseSpaceCompiler.AddExitPoseSpaceBehavior(state);
            }

            ParameterDriverCompiler.SetGroupActive(state, context.ActiveParameter, 0f);
            ParameterDriverCompiler.ResetPoseActiveParameters(state, context.PoseActiveParameters);

            return state;
        }
    }
}
