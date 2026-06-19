using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseMenu))]
    [CanEditMultipleObjects]
    public sealed class PoseMenuEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("installMode", "インストールモード", "Expression Menu への追加方法を選びます。"),
                new PoseTuneFieldLabel("rootMenuName", "ルートメニュー名"),
                new PoseTuneFieldLabel("autoSplitMenu", "メニューを自動分割"),
                new PoseTuneFieldLabel("generateIcons", "アイコンを生成"),
                new PoseTuneFieldLabel("useSubMenusPerGroup", "グループごとにサブメニューを使用"));
        }
    }
}
