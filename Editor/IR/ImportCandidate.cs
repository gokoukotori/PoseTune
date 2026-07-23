using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ImportCandidate
    {
        public string DisplayName = "";
        public PoseGroupKind GroupKind;
        public AnimationClip Clip;
        public string AnimatorPath = "";
        public int SourceLayerIndex;
        public string SourceLayerName = "";
        public string StateName = "";
        public string StatePath = "";
        public string MotionPath = "";
        public PoseImportTarget Target;
        public bool EnabledByDefault = true;
        public float Confidence = 1f;
        public List<string> ConfidenceReasons = new();
        public string DisabledReason = "";
        public bool FromBlendTree;
        public List<BlendTreeChildInfo> BlendTreePath = new();
        public bool HasTrackingBehavior;
        public TrackingPolicyData TrackingPolicy = TrackingPolicyData.DefaultForPose();
        public List<ParameterConditionData> Conditions = new();
        public List<List<ParameterConditionData>> ConditionBranches = new();
        public List<ImportConditionBranchInfo> ConditionBranchInfos = new();
    }

    public sealed class BlendTreeChildInfo
    {
        public string BlendTreeName = "";
        public string BlendParameter = "";
        public float Threshold;
        public Vector2 Position;
        public float TimeScale = 1f;
        public string ChildMotionName = "";
    }

    public sealed class ImportConditionBranchInfo
    {
        public string Source = "";
        public List<ParameterConditionData> Conditions = new();
    }
}
