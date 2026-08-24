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

        [MenuItem("GameObject/PoseTune/テンプレート", false, 30)]
        private static void AddTemplateMenuItem()
        {
            var selected = Selection.activeGameObject;
            var avatar = ResolveAvatar(selected);
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("PoseTune", "VRCAvatarDescriptor を持つ Avatar を選択してください。", "OK");
                return;
            }

            Selection.activeGameObject = CreateTemplate(avatar.gameObject);
        }

        [MenuItem("GameObject/PoseTune/テンプレート", true)]
        private static bool ValidateAddTemplateMenuItem()
        {
            return ResolveAvatar(Selection.activeGameObject) != null;
        }

        public static GameObject CreateTemplate(GameObject avatarRoot)
        {
            var avatar = ResolveAvatar(avatarRoot);
            if (avatar == null)
            {
                throw new System.ArgumentException("Avatar root は VRCAvatarDescriptor を含むか、その配下にある必要があります。", nameof(avatarRoot));
            }

            var root = new GameObject("PoseTune テンプレート");
            Undo.RegisterCreatedObjectUndo(root, "PoseTune テンプレートを追加");
            root.transform.SetParent(avatar.transform, false);
            root.AddComponent<PoseTuneRoot>();
            root.AddComponent<PoseTuneAssistant>();

            var menu = new GameObject("メニュー");
            menu.transform.SetParent(root.transform, false);
            menu.AddComponent<PoseMenu>();

            var groups = new GameObject(PoseGroupsRootName);
            groups.transform.SetParent(root.transform, false);
            CreateDefaultGroup(groups.transform, PoseGroupKind.Standing, DefaultDisplayName(PoseGroupKind.Standing), 0);
            CreateDefaultGroup(groups.transform, PoseGroupKind.Chair, DefaultDisplayName(PoseGroupKind.Chair), 10);
            CreateDefaultGroup(groups.transform, PoseGroupKind.Floor, DefaultDisplayName(PoseGroupKind.Floor), 20);
            CreateDefaultGroup(groups.transform, PoseGroupKind.Prone, DefaultDisplayName(PoseGroupKind.Prone), 30);
            CreateDefaultGroup(groups.transform, PoseGroupKind.Supine, DefaultDisplayName(PoseGroupKind.Supine), 40);

            var tracking = new GameObject("トラッキングポリシー");
            tracking.transform.SetParent(root.transform, false);
            tracking.AddComponent<PoseTrackingPolicy>();

            var height = new GameObject("高さ調整");
            height.transform.SetParent(root.transform, false);
            height.AddComponent<PoseHeightAdjust>();

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
            var go = new GameObject(displayName);
            Undo.RegisterCreatedObjectUndo(go, "ポーズグループを追加");
            go.transform.SetParent(parent, false);
            var group = go.AddComponent<PoseGroup>();
            group.kind = kind;
            group.displayName = displayName;
            group.parameterName = "";
            group.menuOrder = menuOrder;
            return group;
        }

        private static VRCAvatarDescriptor ResolveAvatar(GameObject selected)
        {
            if (selected == null)
            {
                return null;
            }

            return selected.GetComponent<VRCAvatarDescriptor>()
                   ?? selected.GetComponentInParent<VRCAvatarDescriptor>(true)
                   ?? selected.GetComponentInChildren<VRCAvatarDescriptor>(true);
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
            var go = new GameObject(clip != null ? clip.name : "新規ポーズ");
            Undo.RegisterCreatedObjectUndo(go, "PoseClip を追加");
            go.transform.SetParent(group.transform, false);
            var pose = go.AddComponent<PoseClip>();
            pose.clip = clip;
            pose.displayName = clip != null ? ObjectNames.NicifyVariableName(clip.name) : "新規ポーズ";
            EditorUtility.SetDirty(group);
            return pose;
        }
    }
}
