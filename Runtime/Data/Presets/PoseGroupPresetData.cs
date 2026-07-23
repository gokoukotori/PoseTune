using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseGroupPresetData
    {
        [InspectorName("Group Stable GUID")]
        public string groupStableGuid = "";
        [InspectorName("種類")]
        public PoseGroupKind kind;
        [InspectorName("表示名")]
        public string displayName = "";
        [InspectorName("パラメータ名")]
        public string parameterName = "";
        [InspectorName("メニュー順")]
        public int menuOrder;
        [InspectorName("アイコン")]
        public Texture2D icon;
        [InspectorName("排他")]
        public bool exclusive = true;
        [InspectorName("保存")]
        public bool saved = true;
        [InspectorName("同期")]
        public bool synced = true;
        [InspectorName("有効化モード")]
        public PoseGroupActivationMode activationMode = PoseGroupActivationMode.ManualAndAuto;
        [InspectorName("自動時のポーズ選択")]
        public AutoPoseSelectionMode autoPoseSelectionMode = AutoPoseSelectionMode.InitialPoseOnly;
        [InspectorName("自動コンテキストプロファイル")]
        public AutoContextProfile autoContextProfile = AutoContextProfile.Standard;
        [InspectorName("トラッキング制御を生成")]
        public bool emitTrackingControl = true;
        [InspectorName("トラッキングポリシー")]
        public PoseTrackingPolicyPresetData trackingPolicy = new();
        [InspectorName("アイコン生成を抑止")]
        public bool suppressIconGeneration;
        [InspectorName("グループ条件")]
        public List<ParameterConditionData> groupConditions = new();
        [InspectorName("ポーズ空間")]
        public PoseSpacePolicy poseSpace = new();
        [InspectorName("ポーズ")]
        public List<PoseClipPresetData> poses = new();
    }
}
