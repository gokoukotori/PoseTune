using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class PoseMenuPresetData
    {
        [InspectorName("ルートメニュー名")]
        public string rootMenuName = "PoseTune";
        [InspectorName("メニューを自動分割")]
        public bool autoSplitMenu = true;
        [InspectorName("インストールモード")]
        public MenuInstallMode installMode = MenuInstallMode.AppendToRoot;
        [InspectorName("アイコンを生成")]
        public bool generateIcons = true;
        [InspectorName("グループごとにサブメニューを使用")]
        public bool useSubMenusPerGroup = true;
        [InspectorName("寝姿勢メニュー配置")]
        public LyingMenuLayout lyingMenuLayout = LyingMenuLayout.CombinedLyingMenu;
    }
}
