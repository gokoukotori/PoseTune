using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiAuthoringObjectUtility
    {
        public static GameObject EnsureChild(
            Transform parent,
            string name,
            KawaiiMigrationReport report,
            string kind,
            string undoName)
        {
            var existing = parent.Cast<Transform>().FirstOrDefault(child => child.name == name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = CreateChild(parent, name, undoName);
            report.Created(go, kind);
            return go;
        }

        public static GameObject CreateChild(Transform parent, string name, string undoName)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, undoName);
            Undo.SetTransformParent(go.transform, parent, undoName);
            return go;
        }

        public static IEnumerable<T> OwnedComponents<T>(PoseTuneRoot root) where T : Component
        {
            return root.GetComponentsInChildren<T>(true)
                .Where(component => component != null && component.GetComponentInParent<PoseTuneRoot>(true) == root);
        }

        public static string SafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        public static void RecordPrefabModifications(Object target)
        {
            if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }
    }
}
