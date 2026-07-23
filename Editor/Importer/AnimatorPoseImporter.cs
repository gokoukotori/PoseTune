using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ImportAnalysisOptions
    {
        public PoseImportTarget target = PoseImportTarget.BaseLayer;
        public bool importStand = true;
        public bool importCrouch = true;
        public bool importProne = true;
        public bool createDisabledCandidates = true;
        public bool importActionLayer;
        public float minConfidenceForDefaultSelection;
        internal int excludedActionLayerCandidateCount;
    }

    public sealed class AnimatorPoseImporter
    {
        public List<ImportCandidate> Analyze(RuntimeAnimatorController controller)
        {
            return Analyze(controller, new ImportAnalysisOptions());
        }

        public List<ImportCandidate> Analyze(RuntimeAnimatorController controller, ImportAnalysisOptions options)
        {
            var result = new List<ImportCandidate>();
            options ??= new ImportAnalysisOptions();
            options.excludedActionLayerCandidateCount = 0;
            if (controller is not AnimatorController animatorController)
            {
                return result;
            }

            var seen = new HashSet<string>();
            for (var layerIndex = 0; layerIndex < animatorController.layers.Length; layerIndex++)
            {
                var layer = animatorController.layers[layerIndex];
                if (!options.importActionLayer && AnimatorPoseCandidateHeuristics.IsActionLayerName(layer.name))
                {
                    options.excludedActionLayerCandidateCount += AnimatorPoseCandidateCollector.CountPoseCandidates(
                        layer.stateMachine,
                        layer.name,
                        new HashSet<string>());
                    continue;
                }

                AnimatorPoseCandidateCollector.TraverseStateMachine(
                    layer.stateMachine,
                    layer.name,
                    layerIndex,
                    layer.name,
                    result,
                    seen,
                    options,
                    new List<List<ParameterConditionData>>());
            }

            return result
                .OrderBy(c => c.GroupKind)
                .ThenBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public ValidationReport ValidateAnalysisResult(IEnumerable<ImportCandidate> candidates, ImportAnalysisOptions options)
        {
            var report = new ValidationReport();
            var candidateList = (candidates ?? Enumerable.Empty<ImportCandidate>()).ToList();
            options ??= new ImportAnalysisOptions();
            var hasRestrictiveFlags =
                !options.importStand ||
                !options.importCrouch ||
                !options.importProne ||
                (!options.importActionLayer && options.excludedActionLayerCandidateCount > 0);
            if (candidateList.Count == 0 && hasRestrictiveFlags)
            {
                report.Warning(PoseTuneDiagnostics.ImportNoCandidatesMatched.Code, "import 条件に一致する候補がありません。");
            }

            return report;
        }

        public List<PoseClip> ImportSelected(PoseTuneRoot root, IEnumerable<ImportCandidate> candidates)
        {
            return AnimatorPoseImportWriter.ImportSelected(root, candidates);
        }
    }
}
