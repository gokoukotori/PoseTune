using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomEditor(typeof(PoseGroup))]
    [CanEditMultipleObjects]
    public sealed class PoseGroupEditor : PoseTuneLocalizedEditor
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("kind", "種類"),
            new("displayName", "表示名"),
            new("parameterName", "パラメータ名"),
            new("menuOrder", "メニュー順"),
            new("icon", "アイコン"),
            new("includeInBuild", "ビルドに含める"),
            new("exclusive", "排他", "手動選択時に他の排他グループの選択を解除します。"),
            new("saved", "保存"),
            new("synced", "同期"),
            new("activationMode", "有効化モード"),
            new("autoPoseSelectionMode", "自動時のポーズ選択"),
            new("autoContextProfile", "自動コンテキストプロファイル"),
            new("emitTrackingControl", "トラッキング制御を生成"),
            new("groupConditions", "グループ条件"),
            new("poseSpace", "ポーズ空間")
        };

        public override void OnInspectorGUI()
        {
            DrawFields(FieldLabels);
        }
    }
}
