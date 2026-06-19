using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneGeneratedObjectCleaner
    {
        public static void ClearGeneratedObjects(PoseGraph graph)
        {
            if (graph?.AvatarRoot == null || graph.RootComponent == null)
            {
                return;
            }

            var avatarRoot = graph.AvatarRoot;
            var generatedTransforms = avatarRoot.GetComponentsInChildren<PoseTuneGeneratedMarker>(true)
                .Where(marker => marker != null)
                .Where(marker => marker.gameObject != avatarRoot)
                .Where(marker => marker.rootGuid == graph.RootComponent.StableGuid)
                .Select(marker => marker.transform)
                .OrderByDescending(GetDepth)
                .ToArray();

            DestroyTransforms(generatedTransforms, avatarRoot);
        }

        public static void ClearAllGeneratedObjects(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                return;
            }

            var generatedTransforms = avatarRoot.GetComponentsInChildren<PoseTuneGeneratedMarker>(true)
                .Where(marker => marker != null && marker.gameObject != avatarRoot)
                .Select(marker => marker.transform)
                .OrderByDescending(GetDepth)
                .ToArray();

            DestroyTransforms(generatedTransforms, avatarRoot);
        }

        public static void DestroyTransforms(IEnumerable<Transform> transforms, GameObject protectedRoot)
        {
            foreach (var transform in transforms)
            {
                if (transform == null || transform.gameObject == protectedRoot)
                {
                    continue;
                }

                Object.DestroyImmediate(transform.gameObject);
            }
        }

        public static int GetDepth(Transform transform)
        {
            var depth = 0;
            while (transform != null && transform.parent != null)
            {
                depth++;
                transform = transform.parent;
            }

            return depth;
        }
    }
}
