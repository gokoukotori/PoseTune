using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Height Adjust")]
    public sealed class PoseHeightAdjust : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("ビルドに含める")]
        public bool includeInBuild = true;
        [InspectorName("パラメータ名")]
        public string parameterName = "";
        [InspectorName("最小値")]
        public float min = -1f;
        [InspectorName("最大値")]
        public float max = 1f;
        [InspectorName("適用モード")]
        public HeightApplyMode applyMode = HeightApplyMode.RootOrHipsYOffset;
        [InspectorName("高さ Blend プロファイル")]
        public HeightBlendProfile blendProfile = HeightBlendProfile.Standard;
        [InspectorName("低値オフセット")]
        public float lowOffset = -1f;
        [InspectorName("中央オフセット")]
        public float midOffset = 0f;
        [InspectorName("高値オフセット")]
        public float highOffset = 1f;
        [InspectorName("自動補正")]
        public HeightAutoCorrectionMode autoCorrectionMode = HeightAutoCorrectionMode.Disabled;
        [InspectorName("基準 EyeHeight(m)")]
        public float referenceEyeHeightMeters = 1.6f;
        [InspectorName("最大自動オフセット")]
        public float maxAutoOffset = 0.25f;
        [InspectorName("Radial Puppet メニューを生成")]
        [Tooltip("高さの Radial Puppet メニュー項目を生成します。")]
        public bool generateRadialMenu = true;
        [InspectorName("保存")]
        public bool saved = true;
        [InspectorName("同期")]
        public bool synced = true;

        private void OnValidate()
        {
            if (blendProfile != HeightBlendProfile.KawaiiPosing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                parameterName = "FootHeight";
            }

        }
    }
}
