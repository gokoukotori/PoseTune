using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

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

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, undoName);
            go.transform.SetParent(parent, false);
            report.Created(go, kind);
            return go;
        }

        public static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            return selected != null
                ? selected.GetComponent<VRCAvatarDescriptor>()
                  ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                  ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true)
                : null;
        }
    }
}
