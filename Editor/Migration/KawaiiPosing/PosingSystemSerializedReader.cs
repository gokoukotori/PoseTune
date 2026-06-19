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
            dto.GameObjectPath = TransformPath(component.transform);
            dto.SettingName = String(serialized, "settingName");
            dto.IsIconDisabled = Bool(serialized, "isIconDisabled");
            dto.IsIconSmall = Bool(serialized, "isIconSmall");
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
            var motion = ObjectRef<Motion>(property, "animationClip");
            return new KawaiiAnimationDto
            {
                Index = index,
                Enabled = Bool(property, "enabled", true),
                IsRotate = Bool(property, "isRotate"),
                Rotate = Int(property, "rotate"),
                IsMotionTime = Bool(property, "isMotionTime"),
                MotionTimeParameterName = String(property, "motionTimeParamName"),
                Motion = motion,
                Clip = motion as AnimationClip,
                BlendTree = motion as BlendTree,
                PreviewImage = ObjectRef<Texture2D>(property, "previewImage"),
                AdjustmentClip = ObjectRef<AnimationClip>(property, "adjustmentClip"),
                DisplayName = String(property, "displayName"),
                Initial = Bool(property, "initial"),
                InitialSet = Bool(property, "initialSet"),
                IsCustomIcon = Bool(property, "isCustomIcon"),
                Icon = ObjectRef<Texture2D>(property, "icon"),
                TypeParameterValue = Int(property, "typeParameterValue"),
                SyncedParameterValue = Int(property, "syncdParameterValue")
            };
        }

        private static KawaiiOverrideDto ReadOverride(SerializedProperty property, int index)
        {
            var motion = ObjectRef<Motion>(property, "animationClip");
            return new KawaiiOverrideDto
            {
                Index = index,
                Enabled = Bool(property, "enabled", true),
                IsRotate = Bool(property, "isRotate"),
                Rotate = Int(property, "rotate"),
                IsMotionTime = Bool(property, "isMotionTime"),
                MotionTimeParameterName = String(property, "motionTimeParamName"),
                Motion = motion,
                Clip = motion as AnimationClip,
                BlendTree = motion as BlendTree,
                AdjustmentClip = ObjectRef<AnimationClip>(property, "adjustmentClip"),
                StateType = StateType(property.FindPropertyRelative("stateType"))
            };
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

        private static string TransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "";
            }

            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}
