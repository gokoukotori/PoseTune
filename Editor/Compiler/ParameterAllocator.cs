using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ParameterAllocator
    {
        public ParameterPlan Allocate(PoseGraph graph)
        {
            var builder = new ParameterPlanBuilder();
            var buildableGroups = PoseGraphBuildFilter.BuildableGroups(graph).ToList();
            var poseSelection = PoseSelectionPlanner.Build(graph);
            builder.Build().PoseSelection = poseSelection;
            builder.AddInt(graph.RootComponent.Parameter(PoseTuneNames.Mode))
                .Saved()
                .DefaultValue((float)graph.RootComponent.defaultMode);
            if (graph.HasPoseOptions)
            {
                AddLockOption(builder, graph.RootComponent.Parameter(PoseTuneNames.LockHead),
                    graph.Options != null && graph.Options.lockHead);
                AddLockOption(builder, graph.RootComponent.Parameter(PoseTuneNames.LockHands),
                    graph.Options != null && graph.Options.lockHands);
                AddLockOption(builder, graph.RootComponent.Parameter(PoseTuneNames.LockFeet),
                    graph.Options != null && graph.Options.lockFeet);
                AddLockOption(builder, graph.RootComponent.Parameter(PoseTuneNames.LocomotionLock),
                    graph.Options != null && graph.Options.locomotionLock);
            }

            if (NeedsTrackingArbiter(graph))
            {
                foreach (var group in buildableGroups.Where(group => RequiresTrackingVote(graph, group)))
                {
                    builder.AddNotSyncedInt(PoseTuneNames.TrackingVoteParameter(group))
                        .LocalOnly()
                        .AnimatorOnly();
                }

                foreach (var part in TrackingArbiterCompiler.RequiredParts(graph))
                {
                    builder.AddNotSyncedBool(PoseTuneNames.TrackingResetParameter(part))
                        .LocalOnly()
                        .AnimatorOnly();
                }
            }

            if (PoseRefreshLayerCompiler.IsNeeded(graph))
            {
                builder.AddNotSyncedBool(PoseTuneNames.PoseRefreshParameter)
                    .LocalOnly()
                    .AnimatorOnly();
            }

            if (PoseTuneCompilerRules.NeedsSupineFlag(graph))
            {
                builder.AddBool(graph.RootComponent.Parameter(PoseTuneNames.SupineFlag))
                    .Saved();
            }

            if (graph.HasGoroneSystemExGuard)
            {
                builder.AddNotSyncedInt(GoroneSystemExDetector.VrcSupineParameter)
                    .LocalOnly();
            }

            foreach (var channel in poseSelection.Channels)
            {
                builder.AddInt(channel.ParameterName)
                    .Saved(channel.Saved)
                    .LocalOnly(!channel.Synced)
                    .DefaultValue(channel.DefaultValue);
            }

            foreach (var group in buildableGroups)
            {
                if (group.Poses.Count > 0)
                {
                    foreach (var parameterName in PoseTuneLayerNaming.GroupActiveParameters(group))
                    {
                        builder.AddNotSyncedFloat(parameterName)
                            .LocalOnly()
                            .AnimatorOnly();
                    }
                }

                if (NeedsManualCommitGuard(graph, buildableGroups, group, poseSelection))
                {
                    foreach (var pose in group.Poses)
                    {
                        builder.AddNotSyncedBool(PoseTuneNames.PoseActiveParameter(pose))
                            .LocalOnly()
                            .AnimatorOnly();
                    }
                }
            }

            // Parameters PoseTune generates and syncs come first, so a pose motion that reads the same name
            // reuses them regardless of pose order instead of turning them into unsynced parameters.
            if (PoseTuneCompilerRules.AppliesHeightOffset(graph))
            {
                ConfigureHeight(
                    builder.AddFloat(PoseTuneNames.HeightParameter(graph.RootComponent, graph.HeightAdjust)),
                    graph);
            }

            foreach (var pose in graph.Poses)
            {
                AddGeneratedMotionTimeParameter(builder, graph, pose);
            }

            // Names only a pose's own motion reads belong to the avatar; keep them out of MA auto-rename.
            foreach (var pose in graph.Poses)
            {
                if (pose.MotionTime?.mode == MotionTimeMode.UseAnimatorStateTimeParameter &&
                    !string.IsNullOrWhiteSpace(pose.MotionTime.parameterName))
                {
                    AddAvatarSharedFloat(builder, pose.MotionTime.parameterName);
                }

                foreach (var parameter in BlendTreeParameterCollector.Collect(pose.SourceMotion))
                {
                    AddAvatarSharedFloat(builder, parameter);
                }
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

        private static void AddLockOption(ParameterPlanBuilder builder, string name, bool enabled)
        {
            builder.AddBool(name)
                .Saved()
                .LocalOnly()
                .DefaultValue(enabled ? 1f : 0f);
        }

        private static void ConfigureHeight(ParameterPlanEntryBuilder entry, PoseGraph graph)
        {
            entry.Saved(graph.HeightAdjust.saved)
                .LocalOnly(!graph.HeightAdjust.synced)
                .DefaultValue(0.5f);
        }

        private static void AddAvatarSharedFloat(ParameterPlanBuilder builder, string name)
        {
            builder.AddNotSyncedFloatIfMissing(name)
                .LocalOnly()
                .AnimatorOnly()
                .SharedWithAvatar();
        }

        private static void AddGeneratedMotionTimeParameter(
            ParameterPlanBuilder builder,
            PoseGraph graph,
            PoseDefinition pose)
        {
            if (pose.MotionTime == null)
            {
                return;
            }

            switch (pose.MotionTime.mode)
            {
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
                        ConfigureHeight(builder.AddFloatIfMissing(name), graph);
                    }

                    break;
            }
        }

        private static bool NeedsManualCommitGuard(
            PoseGraph graph,
            IEnumerable<PoseGroupDefinition> buildableGroups,
            PoseGroupDefinition group,
            PoseSelectionPlan poseSelection)
        {
            return group != null &&
                   group.Exclusive &&
                   PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group) &&
                   poseSelection.ExclusiveResetParameterNames(
                       graph.RootComponent,
                       buildableGroups,
                       group).Count > 0;
        }

        internal static bool NeedsGeneratedHeightParameter(PoseGraph graph)
        {
            return graph.RootComponent.enableHeightAdjust &&
                   PoseTuneAuthoringInclusion.Includes(graph.HeightAdjust) &&
                   (graph.HeightAdjust.applyMode != HeightApplyMode.Disabled ||
                    graph.Poses.Any(pose => pose.MotionTime != null &&
                                            pose.MotionTime.mode == MotionTimeMode.UseGeneratedHeightParameter));
        }

        internal static bool NeedsTrackingArbiter(PoseGraph graph)
        {
            return graph != null &&
                   (graph.HasPoseOptions || TrackingArbiterCompiler.RequiredParts(graph).Count > 0);
        }

        internal static bool RequiresTrackingVote(PoseGraph graph, PoseGroupDefinition group)
        {
            return graph != null &&
                   group != null &&
                   NeedsTrackingArbiter(graph) &&
                   group.EmitTrackingControl &&
                   group.Poses.Any(pose => TrackingArbiterCompiler.ControlledParts(graph, group, pose).Count > 0);
        }

    }
}
