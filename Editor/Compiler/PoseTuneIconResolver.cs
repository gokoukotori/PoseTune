using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneIconResolver
    {
        private readonly PoseTuneIconCacheService iconCache = new();

        public void Apply(PoseGraph graph)
        {
            if (graph == null || graph.RootComponent == null)
            {
                return;
            }

            var canUseIcons = graph.RootComponent.enableIconGeneration &&
                              (graph.Menu == null || graph.Menu.generateIcons);
            if (!canUseIcons)
            {
                ClearIcons(graph);
                return;
            }

            foreach (var pose in graph.Poses)
            {
                pose.Icon = ResolvePoseIcon(graph, pose);
            }

            foreach (var group in graph.Groups)
            {
                group.Icon = ResolveGroupIcon(graph, group);
            }
        }

        public Texture2D ResolvePoseIcon(PoseGraph graph, PoseDefinition pose)
        {
            if (!CanUseIcons(graph) || pose == null || pose.SuppressIconGeneration)
            {
                return null;
            }

            if (pose.Icon != null)
            {
                return pose.Icon;
            }

            if (pose.Source != null && pose.Source.customIcon != null)
            {
                return pose.Source.customIcon;
            }

            var cached = iconCache.LoadCachedThumbnail(graph, pose);
            if (cached != null)
            {
                return cached;
            }

            return null;
        }

        public Texture2D ResolveGroupIcon(PoseGraph graph, PoseGroupDefinition group)
        {
            if (!CanUseIcons(graph) || group == null || group.SuppressIconGeneration)
            {
                return null;
            }

            if (group.Icon != null)
            {
                return group.Icon;
            }

            if (group.Source != null && group.Source.icon != null)
            {
                return group.Source.icon;
            }

            var initialPose = group.Poses.FirstOrDefault(pose => pose.Initial);
            if (initialPose != null)
            {
                var icon = ResolvePoseIcon(graph, initialPose);
                if (icon != null)
                {
                    return icon;
                }
            }

            var firstPose = group.Poses.FirstOrDefault();
            return firstPose != null ? ResolvePoseIcon(graph, firstPose) : null;
        }

        private static bool CanUseIcons(PoseGraph graph)
        {
            return graph?.RootComponent != null &&
                   graph.RootComponent.enableIconGeneration &&
                   (graph.Menu == null || graph.Menu.generateIcons);
        }

        private static void ClearIcons(PoseGraph graph)
        {
            foreach (var pose in graph.Poses)
            {
                pose.Icon = null;
            }

            foreach (var group in graph.Groups)
            {
                group.Icon = null;
            }
        }
    }
}
