using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneObjectIdentity
    {
        private const int HashByteCount = 8;

        public static bool TryGetPersistentId(Object target, out string persistentId)
        {
            persistentId = "";
            if (target == null)
            {
                return false;
            }

            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            var assetGuid = globalObjectId.assetGUID.ToString();
            if (globalObjectId.identifierType == 0 ||
                globalObjectId.targetObjectId == 0 ||
                string.IsNullOrWhiteSpace(assetGuid) ||
                assetGuid.All(character => character == '0'))
            {
                return false;
            }

            persistentId = globalObjectId.ToString();
            return !string.IsNullOrWhiteSpace(persistentId);
        }

        public static bool TryGetPersistentHash(Object target, out string hash)
        {
            if (!TryGetPersistentId(target, out var persistentId))
            {
                hash = "";
                return false;
            }

            hash = Hash(persistentId);
            return true;
        }

        public static string BuildKey(Component component, Transform avatarRoot)
        {
            if (component == null)
            {
                return "unknown";
            }

            var indices = new Stack<int>();
            var current = component.transform;
            while (current != null && current != avatarRoot)
            {
                indices.Push(current.GetSiblingIndex());
                current = current.parent;
            }

            if (avatarRoot != null && current != avatarRoot)
            {
                indices.Clear();
                for (current = component.transform; current != null; current = current.parent)
                {
                    indices.Push(current.GetSiblingIndex());
                }
            }

            var sameType = component.gameObject.GetComponents(component.GetType());
            var componentIndex = Array.IndexOf(sameType, component);
            var raw = component.GetType().FullName + "|" + string.Join("/", indices) + "|" + componentIndex;
            return Hash(raw);
        }

        public static string Hash(string value)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
            return string.Concat(bytes.Take(HashByteCount).Select(valueByte => valueByte.ToString("x2")));
        }
    }
}
