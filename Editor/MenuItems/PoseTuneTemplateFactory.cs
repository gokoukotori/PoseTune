using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneTemplateFactory
    {
        private const string PoseGroupsRootName = "ポーズグループ";

        private static readonly PoseGroupKind[] DefaultGroupKinds =
        {
            PoseGroupKind.Standing,
            PoseGroupKind.Chair,
            PoseGroupKind.Floor,
            PoseGroupKind.Prone,
            PoseGroupKind.Supine
        };

        [MenuItem("GameObject/Gokoukotori/PoseTune/テンプレート", false, 30)]
        private static void AddTemplateMenuItem()
        {
            if (!TryResolveSelectedAvatarOrWarn(out var avatar))
            {
                return;
            }

            Selection.activeGameObject = CreateTemplate(avatar.gameObject);
        }

        [MenuItem("GameObject/Gokoukotori/PoseTune/テンプレート", true)]
        private static bool ValidateAddTemplateMenuItem()
        {
            return PoseTuneHierarchyUtility.ResolveAvatar(Selection.activeGameObject) != null;
        }

        internal static bool TryResolveSelectedAvatarOrWarn(out VRCAvatarDescriptor avatar)
        {
            avatar = PoseTuneHierarchyUtility.ResolveAvatar(Selection.activeGameObject);
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("PoseTune", "VRCAvatarDescriptor を持つ Avatar を選択してください。", "OK");
                return false;
            }

            return true;
        }

        public static GameObject CreateTemplate(GameObject avatarRoot)
        {
            var avatar = PoseTuneHierarchyUtility.ResolveAvatar(avatarRoot);
            if (avatar == null)
            {
                throw new System.ArgumentException("Avatar root は VRCAvatarDescriptor を含むか、その配下にある必要があります。", nameof(avatarRoot));
            }

            var root = PoseTuneAuthoringFactory.CreateChild<PoseTuneRoot>(
                avatar.transform,
                "PoseTune テンプレート",
                "PoseTune テンプレートを追加").gameObject;
            root.AddComponent<PoseTuneAssistant>();

            PoseTuneAuthoringFactory.CreateChild<PoseMenu>(root.transform, "メニュー");

            var groups = new GameObject(PoseGroupsRootName);
            groups.transform.SetParent(root.transform, false);
            foreach (var kind in DefaultGroupKinds)
            {
                CreateDefaultGroup(groups.transform, kind, DefaultDisplayName(kind), (int)kind * 10);
            }

            PoseTuneAuthoringFactory.CreateChild<PoseTrackingPolicy>(root.transform, "トラッキングポリシー");
            PoseTuneAuthoringFactory.CreateChild<PoseHeightAdjust>(root.transform, "高さ調整");

            EditorUtility.SetDirty(root);
            return root;
        }

        public static string DefaultDisplayName(PoseGroupKind kind)
        {
            switch (kind)
            {
                case PoseGroupKind.Standing:
                    return "立ち姿勢";
                case PoseGroupKind.Chair:
                    return "椅子";
                case PoseGroupKind.Floor:
                    return "床";
                case PoseGroupKind.Prone:
                    return "うつ伏せ";
                case PoseGroupKind.Supine:
                    return "仰向け";
                default:
                    return "カスタム";
            }
        }

        internal static Transform FindPoseGroupsRoot(PoseTuneRoot root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(t => t.name == PoseGroupsRootName)
                   ?? root.transform;
        }

        public static PoseGroup CreateDefaultGroup(Transform parent, PoseGroupKind kind, string displayName, int menuOrder = 0)
        {
            var group = PoseTuneAuthoringFactory.CreateChild<PoseGroup>(parent, displayName, "ポーズグループを追加");
            group.kind = kind;
            group.displayName = displayName;
            group.parameterName = "";
            group.menuOrder = menuOrder;
            return group;
        }
    }

    public static class PoseTuneAuthoringFactory
    {
        public static PoseGroup AddPoseGroup(PoseTuneRoot root, PoseGroupKind kind)
        {
            var groupsRoot = PoseTuneTemplateFactory.FindPoseGroupsRoot(root);
            return PoseTuneTemplateFactory.CreateDefaultGroup(
                groupsRoot,
                kind,
                PoseTuneTemplateFactory.DefaultDisplayName(kind),
                (int)kind * 10);
        }

        public static PoseClip AddPoseClip(PoseGroup group, AnimationClip clip)
        {
            var pose = CreateChild<PoseClip>(group.transform, clip != null ? clip.name : "新規ポーズ", "PoseClip を追加");
            pose.clip = clip;
            pose.displayName = clip != null ? ObjectNames.NicifyVariableName(clip.name) : "新規ポーズ";
            EditorUtility.SetDirty(group);
            return pose;
        }

        internal static T CreateChild<T>(Transform parent, string name, string undoName = null) where T : Component
        {
            var go = new GameObject(name);
            if (undoName != null)
            {
                Undo.RegisterCreatedObjectUndo(go, undoName);
            }

            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
