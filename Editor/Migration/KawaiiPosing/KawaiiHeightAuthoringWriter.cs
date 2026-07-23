using System.Linq;
using Gokoukotori.PoseTune;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiHeightAuthoringWriter
    {
        public static void EnsureHeight(
            PoseTuneRoot root,
            KawaiiMigrationOptions options,
            KawaiiMigrationReport report,
            string undoName)
        {
            if (options.footHeightMode == KawaiiFootHeightMode.Off)
            {
                return;
            }

            var height = root.GetComponentsInChildren<PoseHeightAdjust>(true)
                .FirstOrDefault(candidate => candidate.GetComponentInParent<PoseTuneRoot>(true) == root);
            if (height == null)
            {
                var heightObject = KawaiiAuthoringObjectUtility.EnsureChild(
                    root.transform,
                    "高さ調整",
                    report,
                    "Height",
                    undoName);
                height = Undo.AddComponent<PoseHeightAdjust>(heightObject);
            }

            Undo.RecordObject(height, undoName);
            ((Behaviour)height).enabled = true;
            height.includeInBuild = true;
            height.parameterName = "FootHeight";
            height.saved = true;
            height.synced = false;
            height.generateRadialMenu = true;
            if (options.footHeightMode == KawaiiFootHeightMode.StrictHumanoidLevel)
            {
                height.applyMode = HeightApplyMode.HumanoidLevelOffset;
                height.blendProfile = HeightBlendProfile.KawaiiPosing;
                height.lowOffset = 2f;
                height.midOffset = 0f;
                height.highOffset = -2f;
            }
            else
            {
                height.applyMode = HeightApplyMode.RootOrHipsYOffset;
                height.blendProfile = HeightBlendProfile.Standard;
            }

            if (TryResolveSourceParameterMetadata(root, height.parameterName, out var metadata))
            {
                height.saved = metadata.Saved;
                height.synced = metadata.Synced;
                report.Info(
                    PoseTuneDiagnostics.KawaiiFootHeightParameterMetadataApplied.Code,
                    $"FootHeight parameter metadata を source ModularAvatarParameters から反映しました: saved={height.saved}, synced={height.synced}",
                    height);
            }

            report.FootHeightEnabledPoseCount = report.CreatedPoseCount;
            EditorUtility.SetDirty(height);
            KawaiiAuthoringObjectUtility.RecordPrefabModifications(height);
        }

        private static bool TryResolveSourceParameterMetadata(
            PoseTuneRoot root,
            string parameterName,
            out ParameterMetadata metadata)
        {
            metadata = default;
            if (root == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            var avatar = KawaiiAuthoringObjectUtility.ResolveAvatar(root.gameObject);
            if (avatar == null)
            {
                return false;
            }

            foreach (var parameters in avatar.GetComponentsInChildren<ModularAvatarParameters>(true))
            {
                foreach (var parameter in parameters.parameters)
                {
                    if (!MatchesParameter(parameter, parameterName))
                    {
                        continue;
                    }

                    metadata = new ParameterMetadata(
                        parameter.saved,
                        parameter.syncType != ParameterSyncType.NotSynced && !parameter.localOnly);
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesParameter(ParameterConfig parameter, string parameterName)
        {
            var name = parameter.nameOrPrefix?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return parameter.isPrefix
                ? parameterName.StartsWith(name, System.StringComparison.Ordinal)
                : parameterName == name;
        }

        private readonly struct ParameterMetadata
        {
            public readonly bool Saved;
            public readonly bool Synced;

            public ParameterMetadata(bool saved, bool synced)
            {
                Saved = saved;
                Synced = synced;
            }
        }
    }
}
