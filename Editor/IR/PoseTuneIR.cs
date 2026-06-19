using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseGraph
    {
        public PoseTuneRoot RootComponent;
        public VRCAvatarDescriptor AvatarDescriptor;
        public GameObject AvatarRoot;
        public List<PoseGroupDefinition> Groups = new();
        public List<PoseDefinition> Poses = new();
        public PoseMenu Menu;
        public PoseHeightAdjust HeightAdjust;
        public PoseTuneGoroneSystemExCompatibility GoroneSystemExCompatibility;
        public int GoroneSystemExCompatibilityCount;
        public PoseTuneOptions Options = new();
        public bool HasPoseOptions;
        public TrackingPolicyData RootTrackingPolicy = TrackingPolicyData.DefaultForPose();
        public bool HasCustomRootTrackingPolicy;
        public bool RootGenerateResetOnExit = true;
        public bool HasCustomRootGenerateResetOnExit;
        public int RootTrackingPolicyCount;
        public ValidationReport Validation = new();

        public PoseTuneRoot Root => RootComponent;
        public bool HasErrors => Validation != null && Validation.HasErrors;
        public bool HasGoroneSystemExGuard =>
            GoroneSystemExCompatibility != null &&
            GoroneSystemExCompatibility.guardMode != GoroneSystemExGuardMode.Disabled;
    }

    public sealed class PoseGroupDefinition
    {
        public string Id = "";
        public string DisplayName = "";
        public string LayerName = "";
        public PoseGroupKind Kind;
        public string ParameterName = "";
        public int MenuOrder;
        public bool Saved;
        public bool Synced;
        public bool Exclusive;
        public PoseGroupActivationMode ActivationMode;
        public AutoPoseSelectionMode AutoPoseSelectionMode;
        public AutoContextProfile AutoContextProfile = AutoContextProfile.Standard;
        public bool EmitTrackingControl = true;
        public bool SuppressIconGeneration;
        public Texture2D Icon;
        public PoseGroup Source;
        public List<ParameterConditionData> Conditions = new();
        public List<PoseDefinition> Poses = new();
    }

    public sealed class PoseDefinition
    {
        public string Id = "";
        public string DisplayName = "";
        public AnimationClip Clip;
        public Motion SourceMotion;
        public AnimationClip AdjustmentClip;
        public PoseSourceCompatibilityProfile CompatibilityProfile;
        public PoseAdjustmentApplyMode AdjustmentApplyMode = PoseAdjustmentApplyMode.ReplaceCurves;
        public float RootYawOffsetDegrees;
        public float HumanoidOrientationOffsetYDegrees;
        public bool RecenterRootXZToHead;
        public Vector3 RootOffset;
        public Vector3 CameraOffset;
        public Texture2D Icon;
        public int MenuValue;
        public int SourceSyncedParameterValue;
        public bool Initial;
        public bool Loop = true;
        public int MenuOrder;
        public PoseGroupDefinition Group;
        public PoseClipPriority Priority = PoseClipPriority.Normal;
        public PoseClipBlendMode BlendMode = PoseClipBlendMode.Override;
        public bool GenerateResetOnExit = true;
        public TrackingPolicyData TrackingPolicy = TrackingPolicyData.DefaultForPose();
        public bool EmitTrackingControl = true;
        public bool SuppressIconGeneration;
        public bool HasFullBodyTrackingOverride;
        public TrackingPolicyData FullBodyTrackingPolicy = TrackingPolicyData.DefaultForPose();
        public PoseSpacePolicy PoseSpace = new();
        public MotionTimeSettings MotionTime = new();
        public List<ParameterConditionData> Conditions = new();
        public List<List<ParameterConditionData>> ConditionBranches = new();
        public PoseClip Source;

        public int SelectionValue(PoseTuneRoot root)
        {
            if (root != null &&
                root.poseSelectionSyncMode == PoseSelectionSyncMode.CompressedPoseId &&
                SourceSyncedParameterValue > 0)
            {
                return SourceSyncedParameterValue;
            }

            return MenuValue;
        }
    }
}

