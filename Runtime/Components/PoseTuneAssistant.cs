using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Tune Assistant")]
    public sealed class PoseTuneAssistant : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("最後に選択したタブ")]
        public int lastSelectedTab;
        [InspectorName("詳細設定を表示")]
        public bool showAdvanced;
    }
}
