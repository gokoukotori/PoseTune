using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantTrackingTab
    {
        private static readonly Dictionary<int, Object> TrackingSelections = new();
        private static readonly TrackingPolicyInspectorView TrackingPolicyView = new TrackingPolicyInspectorView();

        internal static readonly PoseTuneFieldLabel[] RootFields =
        {
            new("disableWhenFullBodyTracking", "FBT 時に無効化"),
            new("advancedSettings.allowFullBodyTracking", "FBT 互換モードを許可"),
            new("advancedSettings.lockDesktopLowerBodyTracking", "Desktop 下半身固定")
        };

        public static void Draw(PoseTuneRoot root, SerializedObject rootObject)
        {
            PoseTuneInspectorGui.DrawFields(rootObject, RootFields);

            var id = root.GetInstanceID();
            TrackingSelections.TryGetValue(id, out var selected);
            selected ??= root;
            TrackingPolicyView.Draw(root, ref selected);
            TrackingSelections[id] = selected;
        }
    }
}
