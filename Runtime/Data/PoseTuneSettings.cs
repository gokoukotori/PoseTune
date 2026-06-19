using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseTunePreviewSettings
    {
        [InspectorName("サムネイルサイズ")]
        public int thumbnailSize = 256;
        [InspectorName("背景色")]
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    }

    [Serializable]
    public sealed class PoseTuneAdvancedSettings
    {
        [InspectorName("FBT を許可")]
        public bool allowFullBodyTracking;
        [InspectorName("Action Weight 制御")]
        public ActionWeightControlMode actionWeightControlMode = ActionWeightControlMode.Auto;
        [InspectorName("Desktop の下半身を固定")]
        [Tooltip("Desktop mode の Standing pose で腰と足をアニメーション優先にし、Auto-Footsteps による足の上書きを抑止します。")]
        public bool lockDesktopLowerBodyTracking;
        [InspectorName("生成オブジェクトを Build に残す")]
        public bool keepGeneratedObjectsInBuild;
    }

    [Serializable]
    public sealed class PoseTuneOptions
    {
        [InspectorName("頭をロック")]
        public bool lockHead;
        [InspectorName("手をロック")]
        public bool lockHands;
        [InspectorName("足をロック")]
        public bool lockFeet;
        [InspectorName("移動ロック")]
        public bool locomotionLock;
    }
}
