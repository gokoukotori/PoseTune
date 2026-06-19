using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Gorone System EX 互換")]
    public sealed class PoseTuneGoroneSystemExCompatibility : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("ガードモード")]
        public GoroneSystemExGuardMode guardMode = GoroneSystemExGuardMode.LowerBodyPoseGroups;
        [InspectorName("Gorone System EX を必須にする")]
        public bool requireGoroneSystemEx = true;
        [InspectorName("PoseTune レイヤー優先度を上書き")]
        public bool overridePoseTuneLayerPriority = true;
        [InspectorName("PoseTune レイヤー優先度")]
        public int poseTuneLayerPriority = 10;

        private void OnValidate()
        {
            poseTuneLayerPriority = Mathf.Clamp(poseTuneLayerPriority, -1000, 1000);
        }
    }
}
