using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum PoseGroupKind
    {
        [InspectorName("立ち姿勢")]
        Standing,
        [InspectorName("椅子")]
        Chair,
        [InspectorName("床")]
        Floor,
        [InspectorName("うつ伏せ")]
        Prone,
        [InspectorName("仰向け")]
        Supine,
        [InspectorName("カスタム")]
        Custom
    }

    public enum PoseGroupActivationMode
    {
        [InspectorName("手動")]
        Manual,
        [InspectorName("自動")]
        Auto,
        [InspectorName("手動 + 自動")]
        ManualAndAuto
    }

    public enum AutoPoseSelectionMode
    {
        [InspectorName("条件に一致する最優先ポーズ")]
        InitialPoseOnly,
        [InspectorName("グループで選択中のポーズ")]
        SelectedPosePerGroup
    }

    public enum AutoContextProfile
    {
        [InspectorName("標準")]
        Standard,
        [InspectorName("KawaiiPosing Head Height 近似")]
        KawaiiHeadHeightApproximation
    }
}
