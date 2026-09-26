using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class KawaiiPresetSerializedReader
    {
        internal const string InvalidPresetCode = "PT-KAS001";
        internal const string TargetTypeMissingCode = "PT-KAS002";
        internal const string PresetApplyFailedCode = "PT-KAS003";
        internal const string PresetSchemaMissingCode = "PT-KAS004";

        public static bool TryRead(
            Preset preset,
            out KawaiiPosingSystemDto dto,
            out string errorCode,
            out string errorMessage)
        {
            dto = null;
            errorCode = "";
            errorMessage = "";

            if (preset == null || !AssetDatabase.Contains(preset))
            {
                errorCode = InvalidPresetCode;
                errorMessage = "Project 内の KawaiiPosing .preset Asset を選択してください。";
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(preset);
            if (!assetPath.EndsWith(".preset", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = InvalidPresetCode;
                errorMessage = "選択した Asset は .preset ではありません。";
                return false;
            }

            if (!TryResolveTargetType(preset, out var targetType, out var resolveError))
            {
                errorCode = TargetTypeMissingCode;
                errorMessage = resolveError;
                return false;
            }

            GameObject temporaryObject = null;
            try
            {
                temporaryObject = new GameObject("PoseTune Kawaii Preset Reader")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var component = temporaryObject.AddComponent(targetType) as MonoBehaviour;
                if (component == null || !preset.CanBeAppliedTo(component) || !preset.ApplyTo(component))
                {
                    errorCode = PresetApplyFailedCode;
                    errorMessage = "KawaiiPosing Preset を一時 component へ適用できませんでした。";
                    return false;
                }

                dto = PosingSystemSerializedReader.Read(component);
                if (!ValidateAdjustmentSchema(preset, dto, out errorMessage))
                {
                    dto = null;
                    errorCode = PresetSchemaMissingCode;
                    return false;
                }

                dto.SourceComponent = preset;
                dto.ComponentTypeName = preset.GetTargetFullTypeName();
                dto.GameObjectPath = assetPath;
                foreach (var warning in dto.Warnings)
                {
                    if (warning.Context == null || warning.Context == component)
                    {
                        warning.Context = preset;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                dto = null;
                errorCode = PresetApplyFailedCode;
                errorMessage = "KawaiiPosing Preset の読み取りに失敗しました: " + exception.Message;
                return false;
            }
            finally
            {
                if (temporaryObject != null)
                {
                    Object.DestroyImmediate(temporaryObject);
                }
            }
        }

        private static bool TryResolveTargetType(Preset preset, out Type targetType, out string error)
        {
            targetType = null;
            error = "";
            var matches = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                         .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
                         .Where(KawaiiPosingDetector.IsKawaiiOrPosingSystemType)
                         .OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                GameObject temporaryObject = null;
                try
                {
                    temporaryObject = new GameObject("PoseTune Kawaii Preset Type Probe")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    var component = temporaryObject.AddComponent(type);
                    if (component != null && preset.CanBeAppliedTo(component))
                    {
                        matches.Add(type);
                    }
                }
                catch
                {
                    // A different candidate type can still match the Preset.
                }
                finally
                {
                    if (temporaryObject != null)
                    {
                        Object.DestroyImmediate(temporaryObject);
                    }
                }
            }

            if (matches.Count == 1)
            {
                targetType = matches[0];
                return true;
            }

            if (matches.Count > 1)
            {
                error = "Preset を適用可能な KawaiiPosing component 型が複数あります: " +
                        string.Join(", ", matches.Select(type => type.FullName));
                return false;
            }

            error = "Preset の対象型を読み込めません。KawaiiPosing / PosingSystem package とPresetの互換性を確認してください。" +
                    " Target=" + preset.GetTargetFullTypeName();
            return false;
        }

        private static bool ValidateAdjustmentSchema(
            Preset preset,
            KawaiiPosingSystemDto dto,
            out string error)
        {
            error = "";
            var paths = new HashSet<string>(
                preset.PropertyModifications
                    .Where(modification => modification != null && !string.IsNullOrEmpty(modification.propertyPath))
                    .Select(modification => modification.propertyPath),
                StringComparer.Ordinal);
            var excludedPaths = preset.excludedProperties ?? Array.Empty<string>();

            if (!HasApplicablePath(paths, excludedPaths, "defines.Array.size") ||
                !HasApplicablePath(paths, excludedPaths, "overrideDefines.Array.size"))
            {
                error = "Preset に適用可能な defines / overrideDefines の配列情報がありません。";
                return false;
            }

            foreach (var layer in dto.Layers)
            {
                foreach (var animation in layer.Animations)
                {
                    var path = $"defines.Array.data[{layer.Index}].animations.Array.data[{animation.Index}].adjustmentClip";
                    if (!HasApplicablePath(paths, excludedPaths, path))
                    {
                        error = "Preset に適用可能な調整クリップ情報がありません: " + path;
                        return false;
                    }
                }
            }

            foreach (var sourceOverride in dto.Overrides)
            {
                var path = $"overrideDefines.Array.data[{sourceOverride.Index}].adjustmentClip";
                if (!HasApplicablePath(paths, excludedPaths, path))
                {
                    error = "Preset に適用可能な調整クリップ情報がありません: " + path;
                    return false;
                }
            }

            return true;
        }

        private static bool HasApplicablePath(
            ISet<string> propertyPaths,
            IEnumerable<string> excludedPaths,
            string requiredPath)
        {
            return propertyPaths.Contains(requiredPath) &&
                   !excludedPaths.Any(excludedPath =>
                       !string.IsNullOrEmpty(excludedPath) &&
                       (string.Equals(requiredPath, excludedPath, StringComparison.Ordinal) ||
                        requiredPath.StartsWith(excludedPath + ".", StringComparison.Ordinal)));
        }
    }
}
