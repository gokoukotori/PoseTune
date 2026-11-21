using System;
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

            DestroyGeneratedObjects(graph.AvatarRoot, marker => marker.rootKey == graph.RootId);
        }

        public static void ClearAllGeneratedObjects(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                return;
            }

            DestroyGeneratedObjects(avatarRoot, marker => true);
        }

        private static void DestroyGeneratedObjects(GameObject avatarRoot, Func<PoseTuneGeneratedMarker, bool> include)
        {
            var generatedTransforms = avatarRoot.GetComponentsInChildren<PoseTuneGeneratedMarker>(true)
                .Where(marker => marker != null && marker.gameObject != avatarRoot)
                .Where(include)
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
