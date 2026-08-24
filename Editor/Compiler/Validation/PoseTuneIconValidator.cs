using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor;
using System.Collections.Generic;
using System.Linq;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneIconValidator
    {
        public static void Validate(PoseGraph graph, ValidationReport report)
        {
            if (graph?.RootComponent == null ||
                graph.Menu != null && (!graph.Menu.generateIcons || graph.Menu.installMode == MenuInstallMode.None))
            {
                return;
            }

            if (!graph.RootComponent.enableIconGeneration ||
                graph.RootComponent.questLowMemoryMode)
            {
                return;
            }

            var cache = new PoseTuneIconCacheService();
            var probes = EligiblePoses(graph)
                .Select(pose => new PoseCacheProbe(pose, cache.ProbeCachedThumbnail(graph, pose)))
                .ToList();
            var hasLoadedCache = probes.Any(probe => probe.Cache.Status == PoseTuneThumbnailCacheStatus.Loaded);
            var hasInvalidCache = probes.Any(probe => probe.Cache.Status == PoseTuneThumbnailCacheStatus.Invalid);
            var allMissing = probes.Count > 0 && !hasLoadedCache && !hasInvalidCache;

            foreach (var probe in probes)
            {
                switch (probe.Cache.Status)
                {
                    case PoseTuneThumbnailCacheStatus.Loaded:
                        break;
                    case PoseTuneThumbnailCacheStatus.Missing when allMissing:
                        report.Information(PoseTuneDiagnostics.MissingThumbnail.Code,
                            "メニュー用 thumbnail はまだ生成されていません。build ではアイコンなしで生成されます。",
                            probe.Pose.Source);
                        break;
                    case PoseTuneThumbnailCacheStatus.Missing:
                        report.Warning(PoseTuneDiagnostics.MissingThumbnail.Code,
                            "一部のメニュー用 thumbnail が未生成です。build では該当Poseのアイコンなしで生成されます。",
                            probe.Pose.Source);
                        break;
                    case PoseTuneThumbnailCacheStatus.Invalid:
                        report.Warning(PoseTuneDiagnostics.MissingThumbnail.Code,
                            "メニュー用 thumbnail cache を読み込めません。cacheを再生成してください。",
                            probe.Pose.Source);
                        break;
                }
            }
        }

        private static IEnumerable<PoseDefinition> EligiblePoses(PoseGraph graph)
        {
            return PoseGraphBuildFilter.BuildableGroups(graph)
                .Where(group => PoseTuneCompilerRules.AllowsManualControl(graph.RootComponent, group))
                .SelectMany(group => group.Poses)
                .Where(pose => pose?.Source != null &&
                               !pose.SuppressIconGeneration &&
                               pose.Source.customIcon == null)
                .GroupBy(pose => pose.Source.GetInstanceID())
                .Select(group => group.First());
        }

        private readonly struct PoseCacheProbe
        {
            public PoseCacheProbe(PoseDefinition pose, PoseTuneThumbnailCacheProbe cache)
            {
                Pose = pose;
                Cache = cache;
            }

            public PoseDefinition Pose { get; }
            public PoseTuneThumbnailCacheProbe Cache { get; }
        }
    }
}
