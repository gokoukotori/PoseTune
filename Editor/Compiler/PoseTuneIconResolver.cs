using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneIconResolver
    {
        public void Apply(PoseGraph graph)
        {
            if (graph == null)
            {
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
            if (pose == null)
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

            return null;
        }

        public Texture2D ResolveGroupIcon(PoseGraph graph, PoseGroupDefinition group)
        {
            if (group == null)
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

    }
}
