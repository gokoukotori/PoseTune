using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneBuildCleanup
    {
        public static void CleanupAuthoringForBuild(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                return;
            }

            var rootsByGuid = avatarRoot.GetComponentsInChildren<PoseTuneRoot>(true)
                .Where(root => root != null)
                .GroupBy(root => root.StableGuid)
                .ToDictionary(group => group.Key, group => group.First());
            var markers = avatarRoot.GetComponentsInChildren<PoseTuneGeneratedMarker>(true).ToArray();
            var generatedTransforms = markers
                .Where(marker => !ShouldKeepGeneratedObject(marker, rootsByGuid))
                .Select(marker => marker.transform)
                .OrderByDescending(PoseTuneGeneratedObjectCleaner.GetDepth)
                .ToList();
            var authoringTransforms = avatarRoot.GetComponentsInChildren<Component>(true)
                .Where(PoseTuneAuthoringComponentTypes.IsAuthoringComponent)
                .Select(component => component.transform)
                .Distinct()
                .OrderByDescending(PoseTuneGeneratedObjectCleaner.GetDepth)
                .ToList();

            foreach (var root in avatarRoot.GetComponentsInChildren<PoseTuneRoot>(true))
            {
                if (root == null)
                {
                    continue;
                }

                Object.DestroyImmediate(root);
            }

            foreach (var component in avatarRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                if (PoseTuneAuthoringComponentTypes.IsRemovableBuildComponent(component))
                {
                    Object.DestroyImmediate(component);
                }
            }

            PoseTuneGeneratedObjectCleaner.DestroyTransforms(generatedTransforms, avatarRoot);
            RemoveEmptyAuthoringObjects(authoringTransforms, avatarRoot);
        }

        private static void RemoveEmptyAuthoringObjects(IEnumerable<Transform> transforms, GameObject avatarRoot)
        {
            foreach (var transform in transforms)
            {
                if (transform == null || transform.gameObject == avatarRoot)
                {
                    continue;
                }

                if (transform.childCount == 0 && !HasUserComponents(transform.gameObject))
                {
                    Object.DestroyImmediate(transform.gameObject);
                }
            }
        }

        private static bool HasUserComponents(GameObject gameObject)
        {
            return gameObject.GetComponents<Component>()
                .Any(component => component != null && component is not Transform);
        }

        private static bool ShouldKeepGeneratedObject(
            PoseTuneGeneratedMarker marker,
            IReadOnlyDictionary<string, PoseTuneRoot> rootsByGuid)
        {
            if (marker == null || string.IsNullOrWhiteSpace(marker.rootGuid))
            {
                return false;
            }

            return rootsByGuid.TryGetValue(marker.rootGuid, out var root) &&
                   root != null &&
                   root.advancedSettings.keepGeneratedObjectsInBuild;
        }

    }
}
