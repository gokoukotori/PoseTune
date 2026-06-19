using System;
using Gokoukotori.PoseTune.Editor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor.Compiler.Validation
{
    internal static class PoseTuneRootValidator
    {
        private const int MaxParameterNamespaceLength = 64;

        public static bool ValidatePrerequisites(PoseGraph graph, ValidationReport report)
        {
            if (graph.RootComponent == null)
            {
                report.Error(PoseTuneDiagnostics.RootMissing.Code, "PoseTuneRoot が見つかりません。");
                return false;
            }

            if (graph.AvatarDescriptor == null)
            {
                report.Error(PoseTuneDiagnostics.RootOutsideAvatarDescriptor.Code, "PoseTuneRoot は VRCAvatarDescriptor の配下に配置してください。", graph.RootComponent);
            }

            if (!Enum.IsDefined(typeof(PoseTuneTargetLayer), graph.RootComponent.targetLayer))
            {
                report.Error(PoseTuneDiagnostics.UnsupportedTargetLayer.Code, "PoseTune の対象レイヤーがサポートされていません。", graph.RootComponent);
            }

            var animator = graph.AvatarRoot != null ? graph.AvatarRoot.GetComponent<Animator>() : null;
            if (animator == null)
            {
                report.Error(PoseTuneDiagnostics.AvatarAnimatorMissing.Code, "Avatar に Animator がありません。", graph.AvatarRoot);
            }
            else if (animator.avatar == null)
            {
                report.Error(PoseTuneDiagnostics.AvatarAnimatorAvatarMissing.Code, "Avatar の Animator に Avatar が割り当てられていません。", animator);
            }
            else if (!animator.avatar.isHuman)
            {
                report.Error(PoseTuneDiagnostics.AvatarAnimatorNonHumanoid.Code, "Avatar の Animator は Humanoid である必要があります。", animator);
            }

            if (graph.AvatarRoot != null && graph.AvatarRoot.GetComponentsInChildren<PoseTuneRoot>(true).Length > 1)
            {
                report.Error(PoseTuneDiagnostics.MultipleRootComponents.Code, "この Avatar の配下に複数の PoseTuneRoot があります。", graph.RootComponent);
            }

            if (graph.RootComponent != null &&
                !string.IsNullOrWhiteSpace(graph.RootComponent.parameterNamespace) &&
                graph.RootComponent.parameterNamespace.Trim('/').Length > MaxParameterNamespaceLength)
            {
                report.Warning(PoseTuneDiagnostics.RootNamespaceTooLong.Code, "PoseTune のパラメータ名前空間が長すぎます。", graph.RootComponent);
            }

            return true;
        }
    }
}
