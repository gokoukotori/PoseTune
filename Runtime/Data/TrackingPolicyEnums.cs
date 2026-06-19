using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum TrackingMode
    {
        [InspectorName("変更しない")]
        NoChange,
        [InspectorName("トラッキング")]
        Tracking,
        [InspectorName("アニメーション")]
        Animation
    }
}
