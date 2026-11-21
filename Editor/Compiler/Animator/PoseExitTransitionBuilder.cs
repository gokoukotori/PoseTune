using Gokoukotori.PoseTune;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseExitTransitionBuilder
    {
        /// <summary>Exit transitions are added in this order, not in state creation order.</summary>
        private static readonly PoseStateVariantKind[] ExitOrder =
        {
            PoseStateVariantKind.Base,
            PoseStateVariantKind.FullBody,
            PoseStateVariantKind.Desktop,
            PoseStateVariantKind.Vr
        };

        internal static void AddPoseExitTransitions(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariants variants,
            bool hasAutoEntry)
        {
            foreach (var kind in ExitOrder)
            {
                var variant = variants.Get(kind);
                if (variant == null)
                {
                    continue;
                }

                AddPoseExitTransitions(context, pose, variant, hasAutoEntry);
                AddModeInvalidExitTransition(variants, variant);
            }
        }

        /// <summary>Leaves a variant when the mode it plays in no longer applies.</summary>
        private static void AddModeInvalidExitTransition(PoseStateVariants variants, PoseStateVariant variant)
        {
            switch (variant.Kind)
            {
                case PoseStateVariantKind.Base:
                    if (variants.NeedsDesktopVariant)
                    {
                        ModeTransitionCompiler.AddVrModeInvalidExitTransition(variant.State, variant.Handoff);
                    }
                    else if (variants.NeedsPoseSpaceVrVariant)
                    {
                        ModeTransitionCompiler.AddDesktopModeInvalidExitTransition(variant.State, variant.Handoff);
                    }

                    break;
                case PoseStateVariantKind.Desktop:
                    ModeTransitionCompiler.AddDesktopModeInvalidExitTransition(variant.State, variant.Handoff);
                    break;
                case PoseStateVariantKind.Vr:
                    ModeTransitionCompiler.AddVrModeInvalidExitTransition(variant.State, variant.Handoff);
                    break;
            }
        }

        private static void AddPoseExitTransitions(
            PoseLayerBuildContext context,
            PoseDefinition pose,
            PoseStateVariant variant,
            bool hasAutoEntry)
        {
            var root = context.Root;
            var group = context.Group;
            var state = variant.State;
            var cleanup = variant.Handoff;
            if (AutoContextTransitionCompiler.AllowsManualEntry(root, group))
            {
                var toResetByGroup = AnimatorTransitionFactory.Immediate(state, cleanup);
                ModeTransitionCompiler.AddManualModeEntryCondition(toResetByGroup, root);
                AddManualGroupDeselectedCondition(toResetByGroup, context.PoseSelection, pose);
            }

            var toResetByMode = AnimatorTransitionFactory.Immediate(state, cleanup);
            ModeTransitionCompiler.AddModeExitCondition(toResetByMode, root, group);

            if (AutoContextTransitionCompiler.ShouldResetManualPoseOnAutoMode(root, group, hasAutoEntry))
            {
                var toResetByAutoMode = AnimatorTransitionFactory.Immediate(state, cleanup);
                ModeTransitionCompiler.AddAutoModeCondition(toResetByAutoMode, root);
            }

            TrackingGuardCompiler.AddInvalidExitTransitions(
                state,
                cleanup,
                TrackingGuardCompiler.PoseEntryProfile(root, pose, variant.IsFullBody));

            ConditionTransitionCompiler.AddConditionExitTransitions(state, cleanup, pose);
            if (hasAutoEntry)
            {
                AutoContextTransitionCompiler.AddAutoContextExitTransitions(state, cleanup, root, group);
                AutoContextTransitionCompiler.AddSelectedPoseAutoExitTransition(
                    state,
                    cleanup,
                    root,
                    group,
                    pose,
                    context.PoseSelection);
            }
        }

        private static void AddManualGroupDeselectedCondition(
            AnimatorStateTransition transition,
            PoseSelectionPlan poseSelection,
            PoseDefinition pose)
        {
            var binding = poseSelection?.Find(pose);
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                binding?.Value ?? 0,
                binding?.ParameterName ?? "");
        }
    }
}
