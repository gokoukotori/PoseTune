using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneParameterTypeMapper
    {
        public static AnimatorControllerParameterType ToAnimatorType(PoseTuneParameterValueType type)
        {
            switch (type)
            {
                case PoseTuneParameterValueType.Bool:
                    return AnimatorControllerParameterType.Bool;
                case PoseTuneParameterValueType.Int:
                    return AnimatorControllerParameterType.Int;
                case PoseTuneParameterValueType.Float:
                    return AnimatorControllerParameterType.Float;
                default:
                    return AnimatorControllerParameterType.Float;
            }
        }

        public static VRCExpressionParameters.ValueType ToVrcValueType(PoseTuneParameterValueType type)
        {
            switch (type)
            {
                case PoseTuneParameterValueType.Bool:
                    return VRCExpressionParameters.ValueType.Bool;
                case PoseTuneParameterValueType.Int:
                    return VRCExpressionParameters.ValueType.Int;
                default:
                    return VRCExpressionParameters.ValueType.Float;
            }
        }

        public static PoseTuneParameterValueType FromVrcValueType(VRCExpressionParameters.ValueType type)
        {
            switch (type)
            {
                case VRCExpressionParameters.ValueType.Bool:
                    return PoseTuneParameterValueType.Bool;
                case VRCExpressionParameters.ValueType.Int:
                    return PoseTuneParameterValueType.Int;
                default:
                    return PoseTuneParameterValueType.Float;
            }
        }
    }
}
