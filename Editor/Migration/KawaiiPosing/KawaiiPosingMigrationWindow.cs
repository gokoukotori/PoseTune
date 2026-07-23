using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiPosingMigrationWindow : EditorWindow
    {
        private GameObject avatarRoot;
        private Vector2 scroll;
        private KawaiiMigrationOptions options = KawaiiMigrationOptions.Default();
        private KawaiiMigrationReport lastReport;

        public static void Open(GameObject avatar)
        {
            var window = GetWindow<KawaiiPosingMigrationWindow>("KawaiiPosing 移行");
            window.avatarRoot = avatar;
            window.options = KawaiiMigrationOptions.Default();
            window.RefreshDryRun();
            window.Show();
        }

        private void OnGUI()
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = scrollView.scrollPosition;
                avatarRoot = (GameObject)EditorGUILayout.ObjectField("対象 Avatar", avatarRoot, typeof(GameObject), true);
                if (avatarRoot == null)
                {
                    EditorGUILayout.HelpBox("Avatar を選択してください。", MessageType.Info);
                    return;
                }

                var systems = KawaiiPosingDetector.FindSystems(avatarRoot);
                EditorGUILayout.LabelField("検出された PosingSystem", EditorStyles.boldLabel);
                if (systems.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "KawaiiPosing / PosingSystem component が見つかりません。package を導入済みの Avatar を選択してください。",
                        MessageType.Warning);
                }
                else
                {
                    foreach (var system in systems)
                    {
                        EditorGUILayout.ObjectField(system.SettingName, system.Component, typeof(MonoBehaviour), true);
                    }
                }

                DrawOptions();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("ドライラン"))
                    {
                        RefreshDryRun();
                    }

                    using (new EditorGUI.DisabledScope(systems.Count == 0))
                    {
                        if (GUILayout.Button("移行を実行"))
                        {
                            options.confirmSharedSourceObjectMutation = false;
                            if (!ConfirmSharedSourceMutation(systems.Select(system => system.Component).ToArray()))
                            {
                                return;
                            }

                            options.dryRunOnly = false;
                            lastReport = new KawaiiPosingMigrationExecutor().Execute(
                                avatarRoot,
                                systems.Select(system => system.Component),
                                options);
                        }
                    }
                }

                DrawReport();
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("互換オプション", EditorStyles.boldLabel);
            options.createNewPoseTuneRoot = EditorGUILayout.Toggle("新規 PoseTuneRoot", options.createNewPoseTuneRoot);
            using (new EditorGUI.DisabledScope(options.createNewPoseTuneRoot))
            {
                options.existingRoot = (PoseTuneRoot)EditorGUILayout.ObjectField("既存 Root", options.existingRoot, typeof(PoseTuneRoot), true);
            }

            options.preserveSourceParameterNames = EditorGUILayout.Toggle("元パラメータ名を保持", options.preserveSourceParameterNames);
            options.preserveExplicitMenuValues = EditorGUILayout.Toggle("明示メニュー値を保持", options.preserveExplicitMenuValues);
            options.preserveInitialPose = EditorGUILayout.Toggle("初期ポーズを保持", options.preserveInitialPose);
            options.preserveCustomIcons = EditorGUILayout.Toggle("カスタムアイコンを保持", options.preserveCustomIcons);
            options.footHeightMode = SelectableEnumPopup("足の高さ", options.footHeightMode);
            options.blendTreeMode = SelectableEnumPopup("BlendTree 互換", options.blendTreeMode);
            options.rootRecenterMode = SelectableEnumPopup("Root 再中心化", options.rootRecenterMode);
            options.rotationMode = SelectableEnumPopup("回転", options.rotationMode);
            options.adjustmentMode = SelectableEnumPopup("調整", options.adjustmentMode);
            options.motionTimeMode = SelectableEnumPopup("MotionTime", options.motionTimeMode);
            options.overrideImportMode = SelectableEnumPopup("OverrideDefines 取り込み", options.overrideImportMode);
            options.poseSpaceMode = SelectableEnumPopup("PoseSpace 互換", options.poseSpaceMode);
            options.targetLayerMode = SelectableEnumPopup("対象レイヤー", options.targetLayerMode);
            options.selectionSyncMode = PoseSelectionSyncMode.DirectGroupParameter;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Popup("同期方式", 0, new[] { KawaiiMigrationOptionSupport.DisplayName(options.selectionSyncMode) });
            }
            options.addTrackingPolicy = EditorGUILayout.Toggle("TrackingPolicy を追加", options.addTrackingPolicy);
            options.disableWhenFullBodyTracking = EditorGUILayout.Toggle("FBT 時に無効化", options.disableWhenFullBodyTracking);
            options.sourceDisposition = SelectableEnumPopup("移行元の扱い", options.sourceDisposition);
        }

        private bool ConfirmSharedSourceMutation(MonoBehaviour[] sources)
        {
            if (options.sourceDisposition == KawaiiSourceDisposition.KeepUnchanged)
            {
                return EditorUtility.DisplayDialog(
                    "PoseTune Kawaii Migration",
                    "移行元を変更せずに保持します。KawaiiPosing と PoseTune の両方が build に作用し得ます。\n" +
                    "build 前に構成を確認してください。移行を続行しますか？",
                    "続行",
                    "キャンセル");
            }

            var sourceSet = sources.Where(source => source != null).ToHashSet();
            var sharedObjects = sourceSet
                .Select(source => source.gameObject)
                .Distinct()
                .Where(gameObject => IsSharedSourceObject(gameObject, sourceSet))
                .ToArray();
            if (sharedObjects.Length == 0)
            {
                return true;
            }

            var details = string.Join("\n\n", sharedObjects.Select(gameObject =>
                DescribeSharedSourceObject(gameObject, sourceSet)));
            options.confirmSharedSourceObjectMutation = EditorUtility.DisplayDialog(
                "PoseTune Kawaii Migration",
                "移行元 GameObject には他の Component または子階層があります。選択した処理は GameObject 全体へ作用します。\n\n" +
                details + "\n\n続行しますか？",
                "続行",
                "キャンセル");
            return options.confirmSharedSourceObjectMutation;
        }

        private static bool IsSharedSourceObject(GameObject gameObject, System.Collections.Generic.ISet<MonoBehaviour> sources)
        {
            if (gameObject == null || gameObject.transform.childCount > 0)
            {
                return true;
            }

            return gameObject.GetComponents<Component>().Any(component =>
                component is not Transform &&
                (component is not MonoBehaviour behaviour || !sources.Contains(behaviour)));
        }

        private static string DescribeSharedSourceObject(
            GameObject gameObject,
            System.Collections.Generic.ISet<MonoBehaviour> sources)
        {
            var unrelatedComponents = gameObject.GetComponents<Component>()
                .Where(component => component is not Transform &&
                                    (component is not MonoBehaviour behaviour || !sources.Contains(behaviour)))
                .Select(component => component.GetType().FullName)
                .ToArray();
            var childPaths = gameObject.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != gameObject.transform)
                .Select(transform => HierarchyPath(transform))
                .ToArray();
            var lines = new System.Collections.Generic.List<string> { "- " + HierarchyPath(gameObject.transform) };
            if (unrelatedComponents.Length > 0)
            {
                lines.Add("  Components: " + string.Join(", ", unrelatedComponents));
            }

            if (childPaths.Length > 0)
            {
                lines.Add("  Children: " + string.Join(", ", childPaths));
            }

            return string.Join("\n", lines);
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new System.Collections.Generic.Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        private static T SelectableEnumPopup<T>(string label, T value) where T : struct, Enum
        {
            var values = Enum.GetValues(typeof(T))
                .Cast<T>()
                .Where(item => KawaiiMigrationOptionSupport.IsSelectable(item))
                .ToArray();
            if (values.Length == 0)
            {
                return value;
            }

            var index = System.Array.IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            var labels = values.Select(item => KawaiiMigrationOptionSupport.DisplayName(item)).ToArray();
            var next = EditorGUILayout.Popup(label, index, labels);
            return values[Mathf.Clamp(next, 0, values.Length - 1)];
        }

        private void DrawReport()
        {
            if (lastReport == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("レポート", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"移行元: {lastReport.SourceSystemCount}");
            EditorGUILayout.LabelField($"グループ: {lastReport.CreatedGroupCount}");
            EditorGUILayout.LabelField($"ポーズ: {lastReport.CreatedPoseCount}");
            EditorGUILayout.LabelField($"スキップ: {lastReport.SkippedPoseCount}");
            foreach (var issue in lastReport.Issues)
            {
                var type = issue.Severity == KawaiiMigrationSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == KawaiiMigrationSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox($"{issue.Code}: {LocalizedIssueMessage(issue)}", type);
            }
        }

        private string LocalizedIssueMessage(KawaiiMigrationIssue issue)
        {
            if (issue.Code == PoseTuneDiagnostics.KawaiiMigrationOptionsInfo.Code)
            {
                return "移行オプション: " + KawaiiMigrationOptionSupport.DisplaySummary(options);
            }

            if (issue.Code == PoseTuneDiagnostics.KawaiiMigrationSummaryInfo.Code)
            {
                return $"ドライラン: グループ={lastReport.CreatedGroupCount}, ポーズ={lastReport.CreatedPoseCount}";
            }

            return issue.Message;
        }

        private void RefreshDryRun()
        {
            if (avatarRoot == null)
            {
                lastReport = null;
                return;
            }

            var systems = KawaiiPosingDetector.FindSystems(avatarRoot);
            options.dryRunOnly = true;
            lastReport = new KawaiiPosingMigrationExecutor().Execute(
                avatarRoot,
                systems.Select(system => system.Component),
                options);
        }
    }
}
