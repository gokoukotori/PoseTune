using UnityEngine;

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
    }
}
