using System.Collections.Generic;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAutoContextConditionService
    {
        private const float KawaiiStandingUprightMin = 0.75f;
        private const float KawaiiChairUprightMin = 0.55f;
        private const float KawaiiLyingUprightMax = 0.35f;

        public static List<ParameterConditionData> AutoContextConditions(
            PoseTuneRoot root,
            PoseGroupDefinition group)
        {
            return AutoContextConditions(
                root,
                group != null ? group.Kind : PoseGroupKind.Custom,
                group != null ? group.AutoPoseSelectionMode : AutoPoseSelectionMode.InitialPoseOnly,
                group != null ? group.AutoContextProfile : AutoContextProfile.Standard);
        }

        public static List<ParameterConditionData> AutoContextConditions(
            PoseTuneRoot root,
            PoseGroupKind kind,
            AutoPoseSelectionMode autoPoseSelectionMode,
            AutoContextProfile autoContextProfile)
        {
            var useKawaiiHeadHeight =
                autoContextProfile == AutoContextProfile.KawaiiHeadHeightApproximation &&
                autoPoseSelectionMode == AutoPoseSelectionMode.SelectedPosePerGroup;
            switch (kind)
            {
                case PoseGroupKind.Standing:
                    return new List<ParameterConditionData>
                    {
                        BoolCondition("Seated", ConditionOperator.IfNot),
                        FloatCondition("Upright", ConditionOperator.Greater, KawaiiStandingUprightMin)
                    };
                case PoseGroupKind.Chair:
                    if (useKawaiiHeadHeight)
                    {
                        return new List<ParameterConditionData>
                        {
                            BoolCondition("Seated", ConditionOperator.IfNot),
                            FloatCondition("Upright", ConditionOperator.Less, KawaiiStandingUprightMin),
                            FloatCondition("Upright", ConditionOperator.Greater, KawaiiChairUprightMin)
                        };
                    }

                    return new List<ParameterConditionData>
                    {
                        BoolCondition("Seated", ConditionOperator.If)
                    };
                case PoseGroupKind.Floor:
                    if (useKawaiiHeadHeight)
                    {
                        return new List<ParameterConditionData>
                        {
                            BoolCondition("Seated", ConditionOperator.IfNot),
                            FloatCondition("Upright", ConditionOperator.Less, KawaiiChairUprightMin),
                            FloatCondition("Upright", ConditionOperator.Greater, KawaiiLyingUprightMax)
                        };
                    }

                    return new List<ParameterConditionData>
                    {
                        BoolCondition("Seated", ConditionOperator.IfNot),
                        FloatCondition("Upright", ConditionOperator.LessOrEqual, KawaiiStandingUprightMin),
                        FloatCondition("Upright", ConditionOperator.Greater, KawaiiLyingUprightMax)
                    };
                case PoseGroupKind.Prone:
                    return new List<ParameterConditionData>
                    {
                        BoolCondition("Seated", ConditionOperator.IfNot),
                        FloatCondition("Upright", useKawaiiHeadHeight
                            ? ConditionOperator.Less
                            : ConditionOperator.LessOrEqual, KawaiiLyingUprightMax),
                        BoolCondition(SupineFlag(root), ConditionOperator.IfNot)
                    };
                case PoseGroupKind.Supine:
                    return new List<ParameterConditionData>
                    {
                        BoolCondition("Seated", ConditionOperator.IfNot),
                        FloatCondition("Upright", useKawaiiHeadHeight
                            ? ConditionOperator.Less
                            : ConditionOperator.LessOrEqual, KawaiiLyingUprightMax),
                        BoolCondition(SupineFlag(root), ConditionOperator.If)
                    };
                default:
                    return new List<ParameterConditionData>();
            }
        }

        private static string SupineFlag(PoseTuneRoot root)
        {
            return root != null ? root.Parameter(PoseTuneNames.SupineFlag) : "PT/SupineFlag";
        }

        private static ParameterConditionData FloatCondition(string parameter, ConditionOperator op, float value)
        {
            return new ParameterConditionData
            {
                parameter = parameter,
                valueType = ParameterValueType.Float,
                op = op,
                floatValue = value
            };
        }

        private static ParameterConditionData BoolCondition(string parameter, ConditionOperator op)
        {
            return new ParameterConditionData
            {
                parameter = parameter,
                valueType = ParameterValueType.Bool,
                op = op
            };
        }
    }
}
