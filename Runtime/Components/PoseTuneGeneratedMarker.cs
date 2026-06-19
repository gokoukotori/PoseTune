using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("")]
    public sealed class PoseTuneGeneratedMarker : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("Root GUID")]
        public string rootGuid = "";
        [InspectorName("生成バージョン")]
        public string generatedVersion = "";
        [InspectorName("グラフハッシュ")]
        public string graphHash = "";
        [InspectorName("生成日時")]
        public string generatedAt = "";
    }
}
