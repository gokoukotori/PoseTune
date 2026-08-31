using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Tune Root")]
    public sealed class PoseTuneRoot : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("表示名")]
        public string displayName = "PoseTune";
        [InspectorName("パラメータ名前空間")]
        public string parameterNamespace = "PT";
        [InspectorName("ビルドモード")]
        [HideInInspector]
        public PoseTuneBuildMode buildMode = PoseTuneBuildMode.ModularAvatar;
        [InspectorName("対象レイヤー")]
        public PoseTuneTargetLayer targetLayer = PoseTuneTargetLayer.Action;

        [InspectorName("自動コンテキスト切替")]
        public bool enableAutoContextSwitch = true;
        [InspectorName("既定モード")]
        public PoseTuneDefaultMode defaultMode = PoseTuneDefaultMode.Off;
        [InspectorName("ポーズ同期方式")]
        public PoseSelectionSyncMode poseSelectionSyncMode = PoseSelectionSyncMode.DirectGroupParameter;
        [InspectorName("Write Defaults")]
        public PoseWriteDefaultsMode poseWriteDefaultsMode = PoseWriteDefaultsMode.PoseTuneDefault;
        [InspectorName("高さ調整を有効化")]
        public bool enableHeightAdjust = true;
        [InspectorName("Quest / 低メモリモード")]
        public bool questLowMemoryMode;
        [InspectorName("FBT 時に無効化")]
        public bool disableWhenFullBodyTracking = true;

        [InspectorName("詳細設定")]
        public PoseTuneAdvancedSettings advancedSettings = new();
        public string Parameter(string localName)
        {
            var ns = string.IsNullOrWhiteSpace(parameterNamespace) ? "PT" : parameterNamespace.Trim('/');
            return ns + "/" + localName.Trim('/');
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(parameterNamespace))
            {
                parameterNamespace = "PT";
            }
        }
    }
}
