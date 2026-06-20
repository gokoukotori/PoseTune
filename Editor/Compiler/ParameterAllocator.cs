using System;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ParameterAllocator
    {
        public ParameterPlan Allocate(PoseGraph graph)
        {
            var builder = new ParameterPlanBuilder();
            builder.AddInt(graph.RootComponent.Parameter(PoseTuneNames.Mode))
                .Saved()
                .DefaultValue((float)graph.RootComponent.defaultMode);
            if (graph.HasPoseOptions)
            {
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.LockHead))
                    .Saved()
                    .LocalOnly()
                    .DefaultValue(graph.Options != null && graph.Options.lockHead ? 1f : 0f);
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.LockHands))
                    .Saved()
                    .LocalOnly()
                    .DefaultValue(graph.Options != null && graph.Options.lockHands ? 1f : 0f);
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.LockFeet))
                    .Saved()
                    .LocalOnly()
                    .DefaultValue(graph.Options != null && graph.Options.lockFeet ? 1f : 0f);
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.LocomotionLock))
                    .LocalOnly()
                    .DefaultValue(graph.Options != null && graph.Options.locomotionLock ? 1f : 0f);
            }

            if (NeedsSupineFlag(graph))
            {
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.SupineFlag))
                    .Saved();
            }

            if (graph.HasGoroneSystemExGuard)
            {
                builder.AddNotSyncedInt(GoroneSystemExDetector.VrcSupineParameter)
                    .LocalOnly();
            }

            var buildableGroups = PoseGraphBuildFilter.BuildableGroups(graph).ToList();
            foreach (var group in buildableGroups)
            {
                if (PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
                {
                    builder.AddInt(group.ParameterName)
                        .Saved(group.Saved)
                        .LocalOnly(!group.Synced)
                        .DefaultValue(group.Poses.FirstOrDefault(p => p.Initial)?.SelectionValue(graph.RootComponent) ?? 0);
                }

                if (PoseTuneCompilerRules.ControlsActionPlayable(graph.RootComponent) || graph.HasPoseOptions)
                {
                    foreach (var parameterName in PoseTuneLayerNaming.GroupActiveParameters(group))
                    {
                        builder.AddNotSyncedFloat(parameterName)
                            .LocalOnly()
                            .AnimatorOnly();
                    }
                }

                if (NeedsManualCommitGuard(graph, buildableGroups, group))
                {
                    foreach (var pose in group.Poses)
                    {
                        builder.AddNotSyncedBool(PoseTuneNames.PoseActiveParameter(pose))
                            .LocalOnly()
                            .AnimatorOnly();
                    }
                }
            }

            foreach (var pose in graph.Poses)
            {
                AddMotionTimeParameter(builder, graph, pose);
                foreach (var parameter in BlendTreeParameterCollector.Collect(pose.SourceMotion))
                {
                    builder.AddNotSyncedFloatIfMissing(parameter)
                        .LocalOnly()
                        .AnimatorOnly();
                }
            }

            if (graph.RootComponent.enableHeightAdjust && PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                graph.HeightAdjust.applyMode != HeightApplyMode.Disabled)
            {
                builder.AddFloat(PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust))
                    .Saved(graph.HeightAdjust.saved)
                    .LocalOnly(!graph.HeightAdjust.synced || graph.RootComponent.questLowMemoryMode)
                    .DefaultValue(0.5f);
            }

            return builder.Build();
        }

        public ParameterPlan AllocateStrict(PoseGraph graph)
        {
            var plan = Allocate(graph);
            if (plan.DuplicateParameterNames.Count > 0)
            {
                throw new InvalidOperationException(
                    "PoseTune のパラメータ割り当てに重複名があります: " +
                    string.Join(", ", plan.DuplicateParameterNames.Distinct()));
            }

            return plan;
        }

        private static void AddMotionTimeParameter(ParameterPlanBuilder builder, PoseGraph graph, PoseDefinition pose)
        {
            if (pose.MotionTime == null)
            {
                return;
            }

            switch (pose.MotionTime.mode)
            {
                case MotionTimeMode.UseAnimatorStateTimeParameter:
                    if (!string.IsNullOrWhiteSpace(pose.MotionTime.parameterName))
                    {
                        builder.AddNotSyncedFloatIfMissing(pose.MotionTime.parameterName)
                            .LocalOnly()
                            .AnimatorOnly();
                    }

                    break;
                case MotionTimeMode.UseCustomFloatParameter:
                    if (!string.IsNullOrWhiteSpace(pose.MotionTime.parameterName))
                    {
                        builder.AddFloatIfMissing(pose.MotionTime.parameterName)
                            .Saved();
                    }

                    break;
                case MotionTimeMode.UseGeneratedHeightParameter:
                    if (graph.RootComponent.enableHeightAdjust && PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust))
                    {
                        var name = PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust);
                        builder.AddFloatIfMissing(name)
                            .Saved(graph.HeightAdjust.saved)
                            .LocalOnly(!graph.HeightAdjust.synced || graph.RootComponent.questLowMemoryMode)
                            .DefaultValue(0.5f);
                    }

                    break;
            }
        }

        private static bool NeedsSupineFlag(PoseGraph graph)
        {
            return graph.RootComponent.enableAutoContextSwitch &&
                   PoseGraphBuildFilter.BuildableGroups(graph)
                       .Any(g => g.Kind == PoseGroupKind.Prone || g.Kind == PoseGroupKind.Supine);
        }

        private static bool NeedsManualCommitGuard(
            PoseGraph graph,
            System.Collections.Generic.IEnumerable<PoseGroupDefinition> buildableGroups,
            PoseGroupDefinition group)
        {
            return group != null &&
                   group.Exclusive &&
                   PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group) &&
                   buildableGroups.Any(other =>
                       other != group &&
                       other.Exclusive &&
                       PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, other));
        }

        internal static bool NeedsGeneratedHeightParameter(PoseGraph graph)
        {
            return graph.RootComponent.enableHeightAdjust &&
                   PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                   (graph.HeightAdjust.applyMode != HeightApplyMode.Disabled ||
                    graph.Poses.Any(pose => pose.MotionTime != null &&
                                            pose.MotionTime.mode == MotionTimeMode.UseGeneratedHeightParameter));
        }

    }
}
