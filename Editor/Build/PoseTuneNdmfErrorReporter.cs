using System.Collections.Generic;
using Gokoukotori.PoseTune;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneNdmfErrorReporter
    {
        public static IError ToNdmfError(ValidationIssue issue)
        {
            return new PoseTuneNdmfError(issue);
        }

        public static void Report(ValidationReport report, Object fallbackContext = null)
        {
            if (report == null)
            {
                return;
            }

            foreach (var issue in report.Issues)
            {
                using (ErrorReport.WithContextObject(issue.Context != null ? issue.Context : fallbackContext))
                {
                    ErrorReport.ReportError(ToNdmfError(issue));
                }
            }
        }
    }

    internal sealed class PoseTuneNdmfError : IError
    {
        private readonly ValidationIssue issue;
        private readonly List<ObjectReference> references = new();

        public PoseTuneNdmfError(ValidationIssue issue)
        {
            this.issue = issue;
        }

        public ErrorSeverity Severity => issue.Severity == ValidationSeverity.Error
            ? ErrorSeverity.Error
            : ErrorSeverity.NonFatal;

        public VisualElement CreateVisualElement(ErrorReport report)
        {
            var messageType = Severity == ErrorSeverity.Error
                ? HelpBoxMessageType.Error
                : HelpBoxMessageType.Warning;
            var helpBox = new HelpBox(ToMessage(), messageType);
            helpBox.style.flexGrow = 1f;
            helpBox.style.alignSelf = Align.Stretch;
            helpBox.style.whiteSpace = WhiteSpace.Normal;
            return helpBox;
        }

        public string ToMessage()
        {
            var lines = new List<string>
            {
                "[PoseTune] " + issue.Code + ": " + issue.Message
            };

            var contextLabel = ContextLabel(issue.Context);
            if (!string.IsNullOrWhiteSpace(contextLabel))
            {
                lines.Add("対象: " + contextLabel);
            }

            var fixHint = PoseTuneDiagnostics.FixHint(issue.Code);
            if (!string.IsNullOrWhiteSpace(fixHint))
            {
                lines.Add("対処: " + fixHint);
            }

            return string.Join("\n", lines);
        }

        public void AddReference(ObjectReference obj)
        {
            if (obj != null)
            {
                references.Add(obj);
            }
        }

        private static string ContextLabel(Object context)
        {
            if (context == null)
            {
                return "";
            }

            if (context is Component component)
            {
                return TransformPath(component.transform);
            }

            if (context is GameObject gameObject)
            {
                return TransformPath(gameObject.transform);
            }

            return string.IsNullOrWhiteSpace(context.name) ? context.GetType().Name : context.name;
        }

        private static string TransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "";
            }

            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }
    }
}
