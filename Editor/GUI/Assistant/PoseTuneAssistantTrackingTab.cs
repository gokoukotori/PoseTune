using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantTrackingTab
    {
        private static readonly Dictionary<int, Object> TrackingSelections = new();

        public static void Draw(PoseTuneRoot root)
        {
            root.disableWhenFullBodyTracking = EditorGUILayout.Toggle("FBT 時に無効化", root.disableWhenFullBodyTracking);
            root.advancedSettings.allowFullBodyTracking =
                EditorGUILayout.Toggle("FBT 互換モードを許可", root.advancedSettings.allowFullBodyTracking);
            root.advancedSettings.lockDesktopLowerBodyTracking =
                EditorGUILayout.Toggle("Desktop 下半身固定", root.advancedSettings.lockDesktopLowerBodyTracking);

            var id = root.GetInstanceID();
            TrackingSelections.TryGetValue(id, out var selected);
            selected ??= root;
            new TrackingPolicyInspectorView().Draw(root, ref selected);
            TrackingSelections[id] = selected;
        }
    }
}
