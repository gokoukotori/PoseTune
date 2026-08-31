using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiSystemHandle
    {
        public MonoBehaviour Component;
        public GameObject GameObject;
        public string TypeName = "";
        public string SettingName = "";
        public bool IsKawaiiPosing;
    }

    internal sealed class KawaiiPosingSystemDto
    {
        public Object SourceComponent;
        public string ComponentTypeName = "";
        public string GameObjectPath = "";
        public string SettingName = "";
        public bool IsIconDisabled;
        public bool MergeTrackingControl;
        public bool AutoImportAvatarAnimations;
        public Object ThumbnailPackObject;
        public List<KawaiiLayerDto> Layers = new();
        public List<KawaiiOverrideDto> Overrides = new();
        public List<KawaiiReadWarning> Warnings = new();
    }

    internal sealed class KawaiiLayerDto
    {
        public int Index;
        public string MenuName = "";
        public string Description = "";
        public string StateMachineName = "";
        public string ParameterName = "";
        public Texture2D Icon;
        public int LocomotionTypeValue;
        public List<KawaiiAnimationDto> Animations = new();
    }

    internal sealed class KawaiiAnimationDto
    {
        public int Index;
        public bool Enabled = true;
        public bool IsRotate;
        public int Rotate;
        public bool IsMotionTime;
        public string MotionTimeParameterName = "";
        public Motion Motion;
        public AnimationClip Clip;
        public BlendTree BlendTree;
        public Texture2D PreviewImage;
        public AnimationClip AdjustmentClip;
        public string DisplayName = "";
        public bool Initial;
        public bool InitialSet;
        public bool IsCustomIcon;
        public Texture2D Icon;
        public int TypeParameterValue;
        public int SyncedParameterValue;
    }

    internal sealed class KawaiiOverrideDto
    {
        public int Index;
        public bool Enabled = true;
        public bool IsRotate;
        public int Rotate;
        public bool IsMotionTime;
        public string MotionTimeParameterName = "";
        public Motion Motion;
        public AnimationClip Clip;
        public BlendTree BlendTree;
        public Texture2D PreviewImage;
        public AnimationClip AdjustmentClip;
        public string StateType = "";
    }

    internal sealed class KawaiiReadWarning
    {
        public string Code = "";
        public string Message = "";
        public Object Context;
    }
}
