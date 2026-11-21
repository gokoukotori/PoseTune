using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneHierarchyUtility
    {
        public static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            return selected != null
                ? selected.GetComponent<VRCAvatarDescriptor>()
                  ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                  ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true)
                : null;
        }

        public static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }
    }
}
