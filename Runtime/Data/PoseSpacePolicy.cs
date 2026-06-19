using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseSpacePolicy
    {
        [InspectorName("有効")]
        public bool enabled;
        [InspectorName("適用スコープ")]
        public PoseSpaceScope scope = PoseSpaceScope.All;
        [InspectorName("ポーズ空間に入る")]
        public bool enterPoseSpace = true;
        [InspectorName("固定ディレイ")]
        public bool fixedDelay;
        [InspectorName("ディレイ時間")]
        public float delayTime;
    }
}
