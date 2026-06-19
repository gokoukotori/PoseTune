using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Override Import")]
    public sealed class PoseOverrideImport : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("元 Animator Controller")]
        public RuntimeAnimatorController sourceController;
        [InspectorName("インポート先")]
        public PoseImportTarget target = PoseImportTarget.BaseLayer;
        [InspectorName("立ち姿勢をインポート")]
        [Tooltip("Standing と推定された候補を初期選択します。")]
        public bool importStand = true;
        [InspectorName("しゃがみをインポート")]
        [Tooltip("Floor / crouch / kneel と推定された候補を初期選択します。")]
        public bool importCrouch = true;
        [InspectorName("うつ伏せをインポート")]
        [Tooltip("Prone / Supine と推定された候補を初期選択します。")]
        public bool importProne = true;
        [InspectorName("無効な候補を作成")]
        [Tooltip("filter で落ちる候補も未選択候補として表示します。")]
        public bool createDisabledCandidates = true;
        [InspectorName("Action レイヤーをインポート")]
        [Tooltip("Action を含む名前の Animator layer も解析対象に含めます。")]
        public bool importActionLayer;
        [InspectorName("既定選択の最小信頼度")]
        [Range(0f, 1f)]
        public float minConfidenceForDefaultSelection;
    }
}
