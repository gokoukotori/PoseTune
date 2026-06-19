using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiOverrideAuthoringWriter
    {
        public static void ImportOverrides(
            Transform parent,
            KawaiiPosingSystemDto dto,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            string undoName)
        {
            if (options.overrideImportMode == KawaiiOverrideImportMode.ReportOnly)
            {
                foreach (var item in dto.Overrides)
                {
                    report.Warning(PoseTuneDiagnostics.KawaiiOverrideUnsupported.Code, "overrideDefine は report only です: " + item.StateType, dto.SourceComponent);
                }

                return;
            }

            foreach (var item in dto.Overrides.Where(item => item.Enabled))
            {
                var kind = KawaiiPosingMapper.MapOverrideKind(item.StateType);
                if (kind == PoseGroupKind.Custom && options.overrideImportMode == KawaiiOverrideImportMode.ImportSupportedOnly)
                {
                    report.Warning(PoseTuneDiagnostics.KawaiiOverrideUnsupported.Code, "unsupported override state: " + item.StateType, dto.SourceComponent);
                    continue;
                }

                var group = new GameObject("Override " + SafeName(item.StateType, "Custom"));
                Undo.RegisterCreatedObjectUndo(group, undoName);
                group.transform.SetParent(parent, false);
                var poseGroup = group.AddComponent<PoseGroup>();
                poseGroup.kind = kind;
                poseGroup.displayName = "Override " + SafeName(item.StateType, "Custom");
                poseGroup.menuOrder = 1000 + item.Index;
                var pose = group.AddComponent<PoseClip>();
                pose.displayName = poseGroup.displayName;
                pose.clip = item.Clip;
                pose.sourceMotion = item.Motion != null ? item.Motion : item.Clip;
                pose.adjustmentClip = item.AdjustmentClip;
                pose.loop = KawaiiPoseAuthoringWriter.ResolveSourceLoop(pose.sourceMotion, item.Clip);
                pose.compatibilityProfile = PoseSourceCompatibilityProfile.KawaiiPosing;
                pose.includeInBuild = options.overrideImportMode != KawaiiOverrideImportMode.ImportAllAsCustomDisabled;
                report.Created(group, "Override");
            }
        }

        private static string SafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
