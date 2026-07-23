using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(PoseGroupPresetData))]
    public sealed class PoseGroupPresetDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("groupStableGuid", "Group Stable GUID"),
            new("kind", "種類"),
            new("displayName", "表示名"),
            new("parameterName", "パラメータ名"),
            new("menuOrder", "メニュー順"),
            new("icon", "アイコン"),
            new("exclusive", "排他"),
            new("saved", "保存"),
            new("synced", "同期"),
            new("activationMode", "有効化モード"),
            new("autoPoseSelectionMode", "自動時のポーズ選択"),
            new("autoContextProfile", "自動コンテキストプロファイル"),
            new("emitTrackingControl", "トラッキング制御を生成"),
            new("trackingPolicy", "トラッキングポリシー"),
            new("suppressIconGeneration", "アイコン生成を抑止"),
            new("groupConditions", "グループ条件"),
            new("poseSpace", "ポーズ空間"),
            new("poses", "ポーズ")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
