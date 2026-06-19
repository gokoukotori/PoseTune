using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal sealed class PoseTuneValidationContext
    {
        private PoseTuneValidationContext(
            PoseGraph graph,
            ParameterPlan parameters,
            MenuPlan menu,
            IReadOnlyDictionary<string, string> expectedExpressionParameterTypes,
            IReadOnlyDictionary<string, AnimatorControllerParameterType> expectedAnimatorParameterTypes)
        {
            Graph = graph;
            Parameters = parameters;
            Menu = menu;
            ExpectedExpressionParameterTypes = expectedExpressionParameterTypes;
            ExpectedAnimatorParameterTypes = expectedAnimatorParameterTypes;
        }

        public PoseGraph Graph { get; }

        public ParameterPlan Parameters { get; }

        public MenuPlan Menu { get; }

        public IReadOnlyDictionary<string, string> ExpectedExpressionParameterTypes { get; }

        public IReadOnlyDictionary<string, AnimatorControllerParameterType> ExpectedAnimatorParameterTypes { get; }

        public static PoseTuneValidationContext Create(PoseGraph graph)
        {
            var parameters = new ParameterAllocator().Allocate(graph);
            var menu = new MenuCompiler().Compile(graph, parameters);
            var expectedExpressionTypes = parameters.Parameters
                .Where(CountsAsExpressionParameter)
                .ToDictionary(parameter => parameter.Name, parameter => ToVrcValueTypeName(parameter.SyncType));
            var expectedAnimatorTypes = parameters.Parameters
                .ToDictionary(parameter => parameter.Name, parameter => PoseTuneParameterTypeMapper.ToAnimatorType(parameter.ValueType));

            return new PoseTuneValidationContext(graph, parameters, menu, expectedExpressionTypes, expectedAnimatorTypes);
        }

        public static bool CountsAsExpressionParameter(ParameterDefinition parameter)
        {
            return parameter != null &&
                   !parameter.AnimatorOnly &&
                   parameter.SyncType != PoseTuneParameterSyncType.NotSynced;
        }

        private static string ToVrcValueTypeName(PoseTuneParameterSyncType type)
        {
            switch (type)
            {
                case PoseTuneParameterSyncType.Bool:
                    return "Bool";
                case PoseTuneParameterSyncType.Int:
                    return "Int";
                default:
                    return "Float";
            }
        }
    }
}
