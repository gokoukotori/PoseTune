using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum PoseTuneBuildMode
    {
        [InspectorName("Modular Avatar")]
        ModularAvatar
    }

    public enum PoseTuneTargetLayer
    {
        [InspectorName("Action レイヤー")]
        Action,
        [InspectorName("Base レイヤー")]
        Base
    }

    public enum ActionWeightControlMode
    {
        [InspectorName("自動")]
        Auto,
        [InspectorName("無効")]
        Disabled
    }

    public enum PoseTuneDefaultMode
    {
        [InspectorName("オフ")]
        Off = 0,
        [InspectorName("自動")]
        Auto = 1,
        [InspectorName("手動")]
        Manual = 2
    }

    public enum PoseSelectionSyncMode
    {
        [InspectorName("グループパラメータを直接同期")]
        DirectGroupParameter = 0,
        [InspectorName("圧縮 Pose ID")]
        CompressedPoseId = 1,
        [InspectorName("排他グループを共有 Int で同期")]
        SharedExclusivePoseId = 2
    }

    public enum PoseWriteDefaultsMode
    {
        [InspectorName("PoseTune 標準")]
        PoseTuneDefault,
        [InspectorName("Write Defaults Off")]
        ForceOff,
        [InspectorName("Write Defaults On")]
        ForceOn
    }

    public enum PoseTuneParameterSyncType
    {
        [InspectorName("同期しない")]
        NotSynced,
        [InspectorName("Bool")]
        Bool,
        [InspectorName("Int")]
        Int,
        [InspectorName("Float")]
        Float
    }
}
