using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPosingDetector
    {
        private const string KawaiiPosingTypeName = "jp.unisakistudio.kawaiiposing.KawaiiPosing";
        private const string PosingSystemTypeName = "jp.unisakistudio.posingsystem.PosingSystem";

        public static IReadOnlyList<KawaiiSystemHandle> FindSystems(GameObject avatarRoot)
        {
            var result = new List<KawaiiSystemHandle>();
            if (avatarRoot == null)
            {
                return result;
            }

            foreach (var behaviour in avatarRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!IsKawaiiOrPosingSystem(behaviour))
                {
                    continue;
                }

                var type = behaviour.GetType();
                result.Add(new KawaiiSystemHandle
                {
                    Component = behaviour,
                    GameObject = behaviour.gameObject,
                    TypeName = type.FullName ?? type.Name,
                    SettingName = ReadSettingName(behaviour),
                    IsKawaiiPosing = IsTypeOrBase(type, KawaiiPosingTypeName)
                });
            }

            return result;
        }

        public static bool IsKawaiiOrPosingSystem(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            var type = behaviour.GetType();
            if (IsTypeOrBase(type, KawaiiPosingTypeName) || IsTypeOrBase(type, PosingSystemTypeName))
            {
                return true;
            }

            using var serialized = new SerializedObject(behaviour);
            return LooksLikePosingSystemShape(serialized);
        }

        private static bool LooksLikePosingSystemShape(SerializedObject serialized)
        {
            var defines = serialized.FindProperty("defines");
            if (defines == null || !defines.isArray)
            {
                return false;
            }

            if (!HasKawaiiMarker(serialized))
            {
                return false;
            }

            if (defines.arraySize == 0)
            {
                return serialized.FindProperty("overrideDefines")?.isArray == true;
            }

            var first = defines.GetArrayElementAtIndex(0);
            return first != null &&
                   first.FindPropertyRelative("animations")?.isArray == true &&
                   first.FindPropertyRelative("menuName") != null &&
                   first.FindPropertyRelative("stateMachineName") != null &&
                   first.FindPropertyRelative("paramName") != null;
        }

        private static bool HasKawaiiMarker(SerializedObject serialized)
        {
            var settingName = serialized.FindProperty("settingName");
            if (settingName != null &&
                settingName.propertyType == SerializedPropertyType.String &&
                !string.IsNullOrWhiteSpace(settingName.stringValue))
            {
                return true;
            }

            return serialized.FindProperty("isIconDisabled") != null ||
                   serialized.FindProperty("isIconSmall") != null ||
                   serialized.FindProperty("mergeTrackingControl") != null ||
                   serialized.FindProperty("autoImportAvatarAnimations") != null ||
                   serialized.FindProperty("thumbnailPackObject") != null ||
                   serialized.FindProperty("SubmenuRoot") != null ||
                   serialized.FindProperty("overrideDefines")?.isArray == true;
        }

        private static bool IsTypeOrBase(System.Type type, string fullName)
        {
            for (var cursor = type; cursor != null; cursor = cursor.BaseType)
            {
                if (cursor.FullName == fullName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadSettingName(MonoBehaviour behaviour)
        {
            using var serialized = new SerializedObject(behaviour);
            var property = serialized.FindProperty("settingName");
            return property != null ? property.stringValue : "";
        }
    }
}
