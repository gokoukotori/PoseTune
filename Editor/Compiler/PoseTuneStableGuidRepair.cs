using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneStableGuidRepair
    {
        private const string UndoName = "PoseTune Stable GUID を修復";

        public static int Repair(PoseTuneRoot root)
        {
            if (root == null)
            {
                return 0;
            }

            var repaired = 0;
            var avatarDescriptor = root.GetComponentInParent<VRCAvatarDescriptor>(true);
            var avatarRoot = avatarDescriptor != null ? avatarDescriptor.gameObject : root.gameObject;
            repaired += RepairDuplicates(
                avatarRoot.GetComponentsInChildren<PoseTuneRoot>(true),
                component => component.StableGuid,
                component => component.RegenerateStableGuid());
            repaired += RepairDuplicates(
                root.GetComponentsInChildren<PoseGroup>(true),
                component => component.StableGuid,
                component => component.RegenerateStableGuid());
            repaired += RepairDuplicates(
                root.GetComponentsInChildren<PoseClip>(true),
                component => component.StableGuid,
                component => component.RegenerateStableGuid());
            return repaired;
        }

        [MenuItem("Tools/PoseTune/Stable GUIDを修復", true)]
        private static bool ValidateRepairSelection()
        {
            return SelectedRoots().Any();
        }

        [MenuItem("Tools/PoseTune/Stable GUIDを修復")]
        private static void RepairSelection()
        {
            var repaired = 0;
            foreach (var root in SelectedRoots())
            {
                repaired += Repair(root);
            }

            Debug.Log("PoseTune stable GUID repair completed. Repaired: " + repaired);
        }

        private static IEnumerable<PoseTuneRoot> SelectedRoots()
        {
            return Selection.gameObjects
                .SelectMany(gameObject => gameObject.GetComponentsInParent<PoseTuneRoot>(true)
                    .Concat(gameObject.GetComponentsInChildren<PoseTuneRoot>(true)))
                .Where(root => root != null)
                .Distinct();
        }

        private static int RepairDuplicates<T>(
            IEnumerable<T> components,
            System.Func<T, string> stableGuid,
            System.Action<T> regenerate)
            where T : Object
        {
            var repaired = 0;
            var seen = new HashSet<string>();
            foreach (var component in components.Where(component => component != null))
            {
                var current = stableGuid(component);
                if (!string.IsNullOrWhiteSpace(current) && seen.Add(current))
                {
                    continue;
                }

                Undo.RecordObject(component, UndoName);
                do
                {
                    regenerate(component);
                    current = stableGuid(component);
                } while (string.IsNullOrWhiteSpace(current) || seen.Contains(current));

                seen.Add(current);
                EditorUtility.SetDirty(component);
                repaired++;
            }

            return repaired;
        }
    }
}
