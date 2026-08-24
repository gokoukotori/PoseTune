using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using nadena.dev.ndmf;
using nadena.dev.ndmf.ui;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class PoseTuneNdmfErrorReporter
    {
        public static IError ToNdmfError(ValidationIssue issue)
        {
            return new PoseTuneNdmfError(new ValidationIssueGroup(issue));
        }

        public static void Report(ValidationReport report, Object fallbackContext = null)
        {
            if (report == null)
            {
                return;
            }

            foreach (var group in ValidationIssueGrouping.Group(
                         report.Issues.Where(issue => issue.Severity != ValidationSeverity.Information)))
            {
                var error = new PoseTuneNdmfError(group);
                foreach (var context in group.Contexts)
                {
                    error.AddReference(ObjectRegistry.GetReference(context));
                }

                if (group.Contexts.Any())
                {
                    ErrorReport.ReportError(error);
                }
                else
                {
                    using (ErrorReport.WithContextObject(fallbackContext))
                    {
                        ErrorReport.ReportError(error);
                    }
                }
            }
        }
    }

    internal sealed class PoseTuneNdmfError : IError
    {
        private readonly ValidationIssueGroup group;
        private readonly List<ObjectReference> references = new();

        public PoseTuneNdmfError(ValidationIssueGroup group)
        {
            this.group = group;
        }

        public ErrorSeverity Severity
        {
            get
            {
                switch (group.Severity)
                {
                    case ValidationSeverity.Error:
                        return ErrorSeverity.Error;
                    case ValidationSeverity.Warning:
                        return ErrorSeverity.NonFatal;
                    case ValidationSeverity.Information:
                        return ErrorSeverity.Information;
                    default:
                        return ErrorSeverity.NonFatal;
                }
            }
        }

        public VisualElement CreateVisualElement(ErrorReport report)
        {
            var messageType = MessageTypeFor(Severity);
            var helpBox = new HelpBox(ToMessage(), messageType);
            helpBox.style.flexGrow = 1f;
            helpBox.style.alignSelf = Align.Stretch;
            helpBox.style.whiteSpace = WhiteSpace.Normal;

            var root = new VisualElement();
            root.Add(helpBox);
            if (references.Count > 0)
            {
                var foldout = new Foldout
                {
                    text = references.Count == 1 ? "対象" : $"対象（{references.Count}件）",
                    value = false
                };
                foreach (var reference in references)
                {
                    if (ObjectSelector.TryCreate(report, reference, out var selector))
                    {
                        foldout.Add(selector);
                    }
                }

                root.Add(foldout);
            }

            return root;
        }

        public string ToMessage()
        {
            var lines = new List<string>
            {
                "[PoseTune] " + group.Code + ": " + group.Message
            };

            if (group.TargetCount > 1)
            {
                lines.Add($"対象: {group.TargetCount}件");
            }
            else
            {
                var contextLabel = ContextLabel(group.Issues.FirstOrDefault()?.Context);
                if (!string.IsNullOrWhiteSpace(contextLabel))
                {
                    lines.Add("対象: " + contextLabel);
                }
            }

            var fixHint = PoseTuneDiagnostics.FixHint(group.Code);
            if (!string.IsNullOrWhiteSpace(fixHint))
            {
                lines.Add("対処: " + fixHint);
            }

            return string.Join("\n", lines);
        }

        public void AddReference(ObjectReference obj)
        {
            if (obj != null && !references.Contains(obj))
            {
                references.Add(obj);
            }
        }

        private static HelpBoxMessageType MessageTypeFor(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Error:
                case ErrorSeverity.InternalError:
                    return HelpBoxMessageType.Error;
                case ErrorSeverity.Information:
                    return HelpBoxMessageType.Info;
                default:
                    return HelpBoxMessageType.Warning;
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
