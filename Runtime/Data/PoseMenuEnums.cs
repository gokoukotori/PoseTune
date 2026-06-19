using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum LyingMenuLayout
    {
        [InspectorName("寝姿勢メニューに統合")]
        CombinedLyingMenu,
        [InspectorName("グループを個別表示")]
        SeparateGroups
    }

    public enum MenuInstallMode
    {
        [InspectorName("Root に追加")]
        AppendToRoot,
        [InspectorName("Root に直接展開")]
        InlineAtRoot,
        [InspectorName("メニューを生成しない")]
        None
    }
}
