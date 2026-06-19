using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseMenuPresetData))]
    public sealed class PoseMenuPresetDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("rootMenuName", "ルートメニュー名"),
            new("autoSplitMenu", "メニューを自動分割"),
            new("installMode", "インストールモード"),
            new("generateIcons", "アイコンを生成"),
            new("useSubMenusPerGroup", "グループごとにサブメニューを使用"),
            new("lyingMenuLayout", "寝姿勢メニュー配置")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
