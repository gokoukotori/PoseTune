using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public enum ConditionComposition
    {
        [InspectorName("すべて満たす")]
        And,
        [InspectorName("いずれかを満たす")]
        Or
    }

    public enum ParameterValueType
    {
        [InspectorName("Bool")]
        Bool,
        [InspectorName("Int")]
        Int,
        [InspectorName("Float")]
        Float
    }

    public enum ConditionOperator
    {
        [InspectorName("等しい")]
        Equals,
        [InspectorName("等しくない")]
        NotEquals,
        [InspectorName("より大きい")]
        Greater,
        [InspectorName("より小さい")]
        Less,
        [InspectorName("以上")]
        GreaterOrEqual,
        [InspectorName("以下")]
        LessOrEqual,
        [InspectorName("True の場合")]
        If,
        [InspectorName("False の場合")]
        IfNot
    }
}
