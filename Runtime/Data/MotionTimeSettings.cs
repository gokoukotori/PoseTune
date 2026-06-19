using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class MotionTimeSettings
    {
        [InspectorName("モード")]
        public MotionTimeMode mode = MotionTimeMode.None;
        [InspectorName("パラメータ名")]
        public string parameterName = "";
        [InspectorName("Radial Puppet メニューを生成")]
        [Tooltip("Expression Menu から操作できる Float parameter の Radial Puppet を生成します。")]
        public bool generateRadialMenu;
    }
}
