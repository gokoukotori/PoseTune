using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("Gokoukotori/PoseTune/Pose Option")]
    public sealed class PoseOption : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("オプション")]
        public PoseTuneOptions options = new();
    }
}
