using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Menu")]
    public sealed class PoseMenu : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("インストールモード")]
        [Tooltip("Expression Menu への追加方法を選びます。")]
        public MenuInstallMode installMode = MenuInstallMode.AppendToRoot;
        [InspectorName("ルートメニュー名")]
        public string rootMenuName = "PoseTune";
        [InspectorName("メニューを自動分割")]
        public bool autoSplitMenu = true;
        [InspectorName("グループごとにサブメニューを使用")]
        public bool useSubMenusPerGroup = true;
        [InspectorName("寝姿勢メニュー配置")]
        public LyingMenuLayout lyingMenuLayout = LyingMenuLayout.CombinedLyingMenu;
    }
}
