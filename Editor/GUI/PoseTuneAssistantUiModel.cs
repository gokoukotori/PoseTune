using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantUiModel
    {
        public static readonly PoseGroupKind[] AddablePoseGroupKinds =
        {
            PoseGroupKind.Standing,
            PoseGroupKind.Chair,
            PoseGroupKind.Floor,
            PoseGroupKind.Prone,
            PoseGroupKind.Supine,
            PoseGroupKind.Custom
        };

        public static string AddPoseGroupLabel(PoseGroupKind kind)
        {
            switch (kind)
            {
                case PoseGroupKind.Standing:
                    return "立ち姿勢を追加";
                case PoseGroupKind.Chair:
                    return "椅子を追加";
                case PoseGroupKind.Floor:
                    return "床を追加";
                case PoseGroupKind.Prone:
                    return "うつ伏せを追加";
                case PoseGroupKind.Supine:
                    return "仰向けを追加";
                default:
                    return "カスタムを追加";
            }
        }
    }
}
