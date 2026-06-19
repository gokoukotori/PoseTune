using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum PoseClipPriority
    {
        [InspectorName("低")]
        Low,
        [InspectorName("通常")]
        Normal,
        [InspectorName("高")]
        High
    }

    public enum PoseClipBlendMode
    {
        [InspectorName("上書き")]
        Override,
        [InspectorName("加算")]
        Additive
    }

    public enum MotionTimeMode
    {
        [InspectorName("なし")]
        None,
        [InspectorName("Animator State Time パラメータを使用")]
        UseAnimatorStateTimeParameter,
        [InspectorName("生成された高さパラメータを使用")]
        UseGeneratedHeightParameter,
        [InspectorName("カスタム Float パラメータを使用")]
        UseCustomFloatParameter
    }

    public enum PoseSourceCompatibilityProfile
    {
        [InspectorName("なし")]
        None,
        [InspectorName("KawaiiPosing")]
        KawaiiPosing
    }

    public enum GoroneSystemExGuardMode
    {
        [InspectorName("無効")]
        Disabled,
        [InspectorName("下半身ポーズのみ")]
        LowerBodyPoseGroups,
        [InspectorName("すべてのポーズ")]
        AllPoseGroups
    }

    public enum PoseAdjustmentApplyMode
    {
        [InspectorName("カーブを置換")]
        ReplaceCurves,
        [InspectorName("Kawaii 互換加算")]
        AdditiveKawaiiCompatible
    }

    public enum PoseSpaceScope
    {
        [InspectorName("すべて")]
        All,
        [InspectorName("デスクトップのみ")]
        DesktopOnly,
        [InspectorName("VR のみ")]
        VROnly
    }
}
