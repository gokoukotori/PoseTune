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
            return PoseTuneTemplateFactory.DefaultDisplayName(kind) + "を追加";
        }
    }
}
