using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class PoseTuneParameterSimulatorWindow : EditorWindow
    {
        private PoseTuneRoot selectedRoot;
        private int profileIndex;
        private int mode;
        private int vrMode;
        private int trackingType = 3;
        private float upright = 1f;
        private bool seated;
        private bool grounded = true;
        private bool supineFlag;
        private float scaleFactor = 1f;
        private float eyeHeightMeters = 1.6f;
        private Vector2 scroll;

        private static readonly string[] Profiles =
        {
            "Desktop",
            "3pt VR",
            "FBT"
        };

        [MenuItem("Tools/PoseTune/Parameter Simulator")]
        public static void Open()
        {
            GetWindow<PoseTuneParameterSimulatorWindow>("PoseTune Simulator");
        }

        private void OnGUI()
        {
            selectedRoot = (PoseTuneRoot)EditorGUILayout.ObjectField("Root", selectedRoot, typeof(PoseTuneRoot), true);
            var nextProfileIndex = GUILayout.Toolbar(profileIndex, Profiles);
            if (nextProfileIndex != profileIndex)
            {
                profileIndex = nextProfileIndex;
                ApplyProfileDefaults();
            }

            EditorGUILayout.HelpBox(PoseTuneParameterSimulatorModel.StaticEvaluationNotice, MessageType.Info);
            mode = EditorGUILayout.IntSlider("Mode", mode, 0, 2);
            vrMode = EditorGUILayout.IntSlider("VRMode", vrMode, 0, 1);
            trackingType = EditorGUILayout.IntSlider("TrackingType", trackingType, 0, 6);
            upright = EditorGUILayout.Slider("Upright", upright, 0f, 1f);
            seated = EditorGUILayout.Toggle("Seated", seated);
            grounded = EditorGUILayout.Toggle("Grounded", grounded);
            supineFlag = EditorGUILayout.Toggle("Supine Flag", supineFlag);
            scaleFactor = EditorGUILayout.FloatField("ScaleFactor", scaleFactor);
            eyeHeightMeters = EditorGUILayout.FloatField("EyeHeightAsMeters", eyeHeightMeters);

            if (selectedRoot == null)
            {
                EditorGUILayout.HelpBox("PoseTuneRoot を指定してください。", MessageType.Info);
                return;
            }

            DrawResults();
        }

        private void DrawResults()
        {
            var snapshot = Snapshot();
            var service = new PoseTuneEntrySimulationService();
            var evaluator = new PoseTuneConditionEvaluator();
            var graph = new PoseGraphCollector().Collect(selectedRoot);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var group in graph.Groups)
            {
                var auto = service.AutoContextMatches(selectedRoot, group, snapshot);
                EditorGUILayout.LabelField(group.DisplayName, auto ? "Auto context: true" : "Auto context: false");
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var pose in group.Poses)
                    {
                        var branch = evaluator.AnyBranchMatches(pose.ConditionBranches, snapshot);
                        var guard = TrackingGuardCompiler.AllowsEntry(
                            TrackingGuardCompiler.PoseEntryProfile(selectedRoot, pose, false),
                            snapshot);
                        EditorGUILayout.LabelField(pose.DisplayName, branch && guard ? "entry: true" : "entry: false");
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private PoseTuneParameterSnapshot Snapshot()
        {
            return PoseTuneParameterSimulatorModel.CreateSnapshot(
                selectedRoot,
                new PoseTuneParameterSimulationSettings
                {
                    Mode = mode,
                    VRMode = vrMode,
                    TrackingType = trackingType,
                    Upright = upright,
                    Seated = seated,
                    Grounded = grounded,
                    SupineFlag = supineFlag,
                    ScaleFactor = scaleFactor,
                    EyeHeightAsMeters = eyeHeightMeters
                });
        }

        private void ApplyProfileDefaults()
        {
            var profile = profileIndex switch
            {
                1 => PoseTuneParameterSnapshot.ThreePointVr(),
                2 => PoseTuneParameterSnapshot.FullBodyTracking(),
                _ => PoseTuneParameterSnapshot.Desktop()
            };

            vrMode = profile.Int("VRMode");
            trackingType = profile.Int("TrackingType");
        }
    }
}
