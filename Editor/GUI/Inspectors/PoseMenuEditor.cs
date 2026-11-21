using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseMenu))]
    [CanEditMultipleObjects]
    public sealed class PoseMenuEditor : PoseTuneLocalizedEditor
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("installMode", "インストールモード", "Expression Menu への追加方法を選びます。"),
            new("rootMenuName", "ルートメニュー名"),
            new("autoSplitMenu", "メニューを自動分割"),
            new("useSubMenusPerGroup", "グループごとにサブメニューを使用"),
            new("lyingMenuLayout", "寝姿勢メニュー配置",
                "寝姿勢のグループを1つのメニューにまとめるか、グループごとに表示するかを選びます。「グループごとにサブメニューを使用」がオンのときだけ反映されます。")
        };

        public override void OnInspectorGUI()
        {
            DrawFields(FieldLabels);
        }
    }
}
