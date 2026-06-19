using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class PoseTuneAssistantImportTab
    {
        private static readonly Dictionary<int, PoseTuneImportSelectionState> ImportSelections = new();

        public static void Draw(PoseTuneRoot root)
        {
            var importer = root.GetComponentInChildren<PoseOverrideImport>(true);
            if (importer == null)
            {
                if (GUILayout.Button("インポート設定を追加"))
                {
                    var go = new GameObject("インポート");
                    Undo.RegisterCreatedObjectUndo(go, "PoseTune インポート設定を追加");
                    go.transform.SetParent(root.transform, false);
                    importer = go.AddComponent<PoseOverrideImport>();
                }

                return;
            }

            var editor = UnityEditor.Editor.CreateEditor(importer);
            editor.OnInspectorGUI();
            Object.DestroyImmediate(editor);

            var state = ImportSelectionFor(root);
            if (GUILayout.Button("候補を解析") && importer.sourceController != null)
            {
                var poseImporter = new AnimatorPoseImporter();
                var options = new ImportAnalysisOptions
                {
                    target = importer.target,
                    importStand = importer.importStand,
                    importCrouch = importer.importCrouch,
                    importProne = importer.importProne,
                    createDisabledCandidates = importer.createDisabledCandidates,
                    importActionLayer = importer.importActionLayer,
                    minConfidenceForDefaultSelection = importer.minConfidenceForDefaultSelection
                };
                var candidates = poseImporter.Analyze(importer.sourceController, options);
                state.SetCandidates(candidates);
                foreach (var issue in poseImporter.ValidateAnalysisResult(candidates, options).Warnings)
                {
                    PoseTuneLog.Warning($"{issue.Code}: {issue.Message}", importer);
                }
            }

            DrawImportCandidates(state);
            using (new EditorGUI.DisabledScope(state.Count == 0))
            {
                if (GUILayout.Button("選択した候補をインポート"))
                {
                    var poseImporter = new AnimatorPoseImporter();
                    var imported = poseImporter.ImportSelected(root, state.SelectedCandidates());
                    state.SetCandidates(Enumerable.Empty<ImportCandidate>());
                    EditorUtility.DisplayDialog("PoseTune インポート", $"{imported.Count} 個の PoseClip をインポートしました。", "OK");
                }
            }
        }

        private static PoseTuneImportSelectionState ImportSelectionFor(PoseTuneRoot root)
        {
            var id = root.GetInstanceID();
            if (!ImportSelections.TryGetValue(id, out var state))
            {
                state = new PoseTuneImportSelectionState();
                ImportSelections[id] = state;
            }

            return state;
        }

        private static void DrawImportCandidates(PoseTuneImportSelectionState state)
        {
            if (state.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("候補", EditorStyles.boldLabel);
            ImportCandidateTreeView.Draw(state.Candidates);
            EditorGUILayout.Space();
            for (var i = 0; i < state.Count; i++)
            {
                var candidate = state.Candidates[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    state.SetSelected(i, EditorGUILayout.Toggle(state.IsSelected(i), GUILayout.Width(18)));
                    candidate.GroupKind = (PoseGroupKind)EditorGUILayout.EnumPopup(candidate.GroupKind, GUILayout.Width(90));
                    using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(180)))
                    {
                        var label = string.IsNullOrWhiteSpace(candidate.DisabledReason)
                            ? candidate.DisplayName
                            : candidate.DisplayName + " (" + candidate.DisabledReason + ")";
                        EditorGUILayout.LabelField(label);
                        EditorGUILayout.LabelField(ImportCandidateDisplay.Summary(candidate), EditorStyles.wordWrappedMiniLabel);
                        new ImportConditionFoldoutView().Draw(candidate, state, i);
                    }

                    EditorGUILayout.ObjectField(candidate.Clip, typeof(AnimationClip), false);
                }
            }
        }
    }
}
