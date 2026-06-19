using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneParameterSimulationSettings
    {
        public int Mode;
        public int VRMode;
        public int TrackingType = 3;
        public float Upright = 1f;
        public bool Seated;
        public bool Grounded = true;
        public bool SupineFlag;
        public float ScaleFactor = 1f;
        public float EyeHeightAsMeters = 1.6f;
    }

    internal static class PoseTuneParameterSimulatorModel
    {
        public const string StaticEvaluationNotice =
            "この Simulator は PoseTune が生成する entry 条件の静的評価です。VRChat runtime の遷移順や State Behaviour 実行順は保証しません。";

        public static PoseTuneParameterSnapshot CreateSnapshot(
            PoseTuneRoot root,
            PoseTuneParameterSimulationSettings settings)
        {
            settings ??= new PoseTuneParameterSimulationSettings();
            var snapshot = new PoseTuneParameterSnapshot();
            snapshot.SetInt(root != null ? root.Parameter(PoseTuneNames.Mode) : "PT/Mode", settings.Mode);
            snapshot.SetInt("VRMode", settings.VRMode);
            snapshot.SetInt("TrackingType", settings.TrackingType);
            snapshot.SetFloat("Upright", settings.Upright);
            snapshot.SetBool("Seated", settings.Seated);
            snapshot.SetBool("Grounded", settings.Grounded);
            snapshot.SetBool(root != null ? root.Parameter(PoseTuneNames.SupineFlag) : "PT/SupineFlag", settings.SupineFlag);
            snapshot.SetFloat("ScaleFactor", settings.ScaleFactor);
            snapshot.SetFloat("EyeHeightAsMeters", settings.EyeHeightAsMeters);
            return snapshot;
        }
    }
}
