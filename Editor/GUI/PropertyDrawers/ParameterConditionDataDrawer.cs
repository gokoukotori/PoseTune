using Gokoukotori.PoseTune;
using UnityEditor;

namespace Gokoukotori.PoseTune.Editor
{
    [CustomPropertyDrawer(typeof(ParameterConditionData))]
    public sealed class ParameterConditionDataDrawer : PoseTuneLocalizedPropertyDrawer
    {
        private static readonly PoseTuneFieldLabel[] FieldLabels =
        {
            new("parameter", "パラメータ"),
            new("valueType", "値の型"),
            new("op", "比較"),
            new("floatValue", "Float 値"),
            new("intValue", "Int 値"),
            new("boolValue", "Bool 値")
        };

        protected override PoseTuneFieldLabel[] Fields => FieldLabels;
    }
}
