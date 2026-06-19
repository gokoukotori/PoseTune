using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Group")]
    public sealed class PoseGroup : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("種類")]
        public PoseGroupKind kind = PoseGroupKind.Custom;
        [InspectorName("表示名")]
        public string displayName = "ポーズグループ";
        [InspectorName("パラメータ名")]
        public string parameterName = "";
        [InspectorName("メニュー順")]
        public int menuOrder;
        [InspectorName("アイコン")]
        public Texture2D icon;

        [FormerlySerializedAs("enabled")]
        [InspectorName("ビルドに含める")]
        public bool includeInBuild = true;
        [InspectorName("排他")]
        [Tooltip("手動選択時に他の排他グループの選択を解除します。")]
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
        [InspectorName("アイコン生成を抑止")]
        public bool suppressIconGeneration;
        [InspectorName("グループ条件")]
        public List<ParameterConditionData> groupConditions = new();
        [InspectorName("ポーズ空間")]
        public PoseSpacePolicy poseSpace = new();
        [SerializeField, HideInInspector] private StableComponentGuid stableGuid = new();

        public string StableGuid => stableGuid.Value;

        public void SetStableGuid(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                stableGuid.Value = value;
            }
        }

        public void RegenerateStableGuid()
        {
            stableGuid.Regenerate();
        }

        private void OnValidate()
        {
            stableGuid.Ensure();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = ObjectNamesFallback.Nicify(gameObject.name);
            }
        }
    }
}
