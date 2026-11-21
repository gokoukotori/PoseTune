using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PosingSystemSerializedReader
    {
        public static KawaiiPosingSystemDto Read(MonoBehaviour component)
        {
            var dto = new KawaiiPosingSystemDto();
            if (component == null)
            {
                dto.Warnings.Add(new KawaiiReadWarning
                {
                    Code = PoseTuneDiagnostics.KawaiiSerializedReadWarning.Code,
                    Message = "PosingSystem component が null です。"
                });
                return dto;
            }

            using var serialized = new SerializedObject(component);
            dto.SourceComponent = component;
            dto.ComponentTypeName = component.GetType().FullName ?? component.GetType().Name;
            dto.GameObjectPath = PoseTuneHierarchyUtility.HierarchyPath(component.transform);
            dto.SettingName = String(serialized, "settingName");
            dto.IsIconDisabled = Bool(serialized, "isIconDisabled");
            dto.MergeTrackingControl = Bool(serialized, "mergeTrackingControl", true);
            dto.AutoImportAvatarAnimations = Bool(serialized, "autoImportAvatarAnimations");
            dto.ThumbnailPackObject = ObjectRef<Object>(serialized, "thumbnailPackObject");

            var defines = serialized.FindProperty("defines");
            if (defines == null || !defines.isArray)
            {
                dto.Warnings.Add(new KawaiiReadWarning
                {
                    Code = PoseTuneDiagnostics.KawaiiSerializedReadWarning.Code,
                    Message = "defines が読めません。",
                    Context = component
                });
            }
            else
            {
                for (var i = 0; i < defines.arraySize; i++)
                {
                    dto.Layers.Add(ReadLayer(defines.GetArrayElementAtIndex(i), i, dto));
                }
            }

            var overrides = serialized.FindProperty("overrideDefines");
            if (overrides != null && overrides.isArray)
            {
                for (var i = 0; i < overrides.arraySize; i++)
                {
                    dto.Overrides.Add(ReadOverride(overrides.GetArrayElementAtIndex(i), i));
                }
            }

            return dto;
        }

        private static KawaiiLayerDto ReadLayer(SerializedProperty property, int index, KawaiiPosingSystemDto owner)
        {
            var dto = new KawaiiLayerDto
            {
                Index = index,
                MenuName = String(property, "menuName"),
                Description = String(property, "description"),
                StateMachineName = String(property, "stateMachineName"),
                ParameterName = String(property, "paramName"),
                Icon = ObjectRef<Texture2D>(property, "icon"),
                LocomotionTypeValue = Int(property, "locomotionTypeValue")
            };

            var animations = property.FindPropertyRelative("animations");
            if (animations == null || !animations.isArray)
            {
                owner.Warnings.Add(new KawaiiReadWarning
                {
                    Code = PoseTuneDiagnostics.KawaiiSerializedReadWarning.Code,
                    Message = $"Layer {index} の animations が読めません。",
                    Context = owner.SourceComponent
                });
                return dto;
            }

            for (var i = 0; i < animations.arraySize; i++)
            {
                dto.Animations.Add(ReadAnimation(animations.GetArrayElementAtIndex(i), i));
            }

            return dto;
        }

        private static KawaiiAnimationDto ReadAnimation(SerializedProperty property, int index)
        {
            return ReadMotionEntry(property, index, new KawaiiAnimationDto
            {
                DisplayName = String(property, "displayName"),
                Initial = Bool(property, "initial"),
                InitialSet = Bool(property, "initialSet"),
                IsCustomIcon = Bool(property, "isCustomIcon"),
                Icon = ObjectRef<Texture2D>(property, "icon"),
                TypeParameterValue = Int(property, "typeParameterValue"),
                SyncedParameterValue = Int(property, "syncdParameterValue")
            });
        }

        private static KawaiiOverrideDto ReadOverride(SerializedProperty property, int index)
        {
            return ReadMotionEntry(property, index, new KawaiiOverrideDto
            {
                StateType = StateType(property.FindPropertyRelative("stateType"))
            });
        }

        private static T ReadMotionEntry<T>(SerializedProperty property, int index, T dto)
            where T : KawaiiMotionEntryDto
        {
            var motion = ObjectRef<Motion>(property, "animationClip");
            dto.Index = index;
            dto.Enabled = Bool(property, "enabled", true);
            dto.IsRotate = Bool(property, "isRotate");
            dto.Rotate = Int(property, "rotate");
            dto.IsMotionTime = Bool(property, "isMotionTime");
            dto.MotionTimeParameterName = String(property, "motionTimeParamName");
            dto.Motion = motion;
            dto.Clip = motion as AnimationClip;
            dto.BlendTree = motion as BlendTree;
            dto.PreviewImage = ObjectRef<Texture2D>(property, "previewImage");
            dto.AdjustmentClip = ObjectRef<AnimationClip>(property, "adjustmentClip");
            return dto;
        }

        private static string StateType(SerializedProperty property)
        {
            if (property == null)
            {
                return "";
            }

            return property.propertyType switch
            {
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Enum => property.enumNames != null &&
                                               property.enumValueIndex >= 0 &&
                                               property.enumValueIndex < property.enumNames.Length
                    ? property.enumNames[property.enumValueIndex]
                    : property.enumDisplayNames[property.enumValueIndex],
                _ => property.displayName
            };
        }

        private static string String(SerializedObject obj, string path)
        {
            var property = obj.FindProperty(path);
            return property != null ? property.stringValue : "";
        }

        private static string String(SerializedProperty obj, string path)
        {
            var property = obj.FindPropertyRelative(path);
            return property != null ? property.stringValue : "";
        }

        private static bool Bool(SerializedObject obj, string path, bool fallback = false)
        {
            var property = obj.FindProperty(path);
            return property != null ? property.boolValue : fallback;
        }

        private static bool Bool(SerializedProperty obj, string path, bool fallback = false)
        {
            var property = obj.FindPropertyRelative(path);
            return property != null ? property.boolValue : fallback;
        }

        private static int Int(SerializedProperty obj, string path)
        {
            var property = obj.FindPropertyRelative(path);
            return property != null ? property.intValue : 0;
        }

        private static T ObjectRef<T>(SerializedObject obj, string path) where T : Object
        {
            var property = obj.FindProperty(path);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static T ObjectRef<T>(SerializedProperty obj, string path) where T : Object
        {
            var property = obj.FindPropertyRelative(path);
            return property != null ? property.objectReferenceValue as T : null;
        }
    }
}
