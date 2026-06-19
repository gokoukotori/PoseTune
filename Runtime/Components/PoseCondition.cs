using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [AddComponentMenu("PoseTune/Pose Condition")]
    public sealed class PoseCondition : MonoBehaviour, INDMFEditorOnly
    {
        [InspectorName("条件の合成")]
        public ConditionComposition composition = ConditionComposition.And;
        [InspectorName("条件")]
        public List<ParameterConditionData> conditions = new();
    }
}
