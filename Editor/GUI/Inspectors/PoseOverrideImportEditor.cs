using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseOverrideImport))]
    [CanEditMultipleObjects]
    public sealed class PoseOverrideImportEditor : PoseTuneLocalizedEditor
    {
        public override void OnInspectorGUI()
        {
            DrawFields(
                new PoseTuneFieldLabel("sourceController", "元 Animator Controller"),
                new PoseTuneFieldLabel("target", "インポート先"),
                new PoseTuneFieldLabel("importStand", "立ち姿勢をインポート", "Standing と推定された候補を初期選択します。"),
                new PoseTuneFieldLabel("importCrouch", "しゃがみをインポート", "Floor / crouch / kneel と推定された候補を初期選択します。"),
                new PoseTuneFieldLabel("importProne", "うつ伏せをインポート", "Prone / Supine と推定された候補を初期選択します。"),
                new PoseTuneFieldLabel("createDisabledCandidates", "無効な候補を作成", "filter で落ちる候補も未選択候補として表示します。"),
                new PoseTuneFieldLabel("importActionLayer", "Action レイヤーをインポート", "Action を含む名前の Animator layer も解析対象に含めます。"),
                new PoseTuneFieldLabel("minConfidenceForDefaultSelection", "既定選択の最小信頼度"));
        }
    }
}
