using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class ParameterConditionData
    {
        [InspectorName("パラメータ")]
        public string parameter = "";
        [InspectorName("値の型")]
        public ParameterValueType valueType = ParameterValueType.Float;
        [InspectorName("比較")]
        public ConditionOperator op = ConditionOperator.Equals;
        [InspectorName("Float 値")]
        public float floatValue;
        [InspectorName("Int 値")]
        public int intValue;
        [InspectorName("Bool 値")]
        public bool boolValue;
    }
}
