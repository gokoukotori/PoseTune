using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ValidationReport
    {
        public List<ValidationIssue> Errors = new();
        public List<ValidationIssue> Warnings = new();
        public List<ValidationIssue> Informations = new();

        public bool HasErrors => Errors.Count > 0;
        public IEnumerable<ValidationIssue> Issues => Errors.Concat(Warnings).Concat(Informations);

        public void Error(string code, string message, Object context = null)
        {
            Errors.Add(new ValidationIssue(ValidationSeverity.Error, code, message, context));
        }

        public void Warning(string code, string message, Object context = null)
        {
            Warnings.Add(new ValidationIssue(ValidationSeverity.Warning, code, message, context));
        }

        public void Information(string code, string message, Object context = null)
        {
            Informations.Add(new ValidationIssue(ValidationSeverity.Information, code, message, context));
        }

        internal void Add(ValidationIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            switch (issue.Severity)
            {
                case ValidationSeverity.Error:
                    Errors.Add(issue);
                    break;
                case ValidationSeverity.Warning:
                    Warnings.Add(issue);
                    break;
                case ValidationSeverity.Information:
                    Informations.Add(issue);
                    break;
            }
        }
    }

    public enum ValidationSeverity
    {
        Error = 0,
        Warning = 1,
        Information = 2
    }

    public sealed class ValidationIssue
    {
        public readonly ValidationSeverity Severity;
        public readonly string Code;
        public readonly string Message;
        public readonly Object Context;

        public ValidationIssue(ValidationSeverity severity, string code, string message, Object context)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Context = context;
        }
    }

    internal sealed class ValidationIssueGroup
    {
        private readonly List<ValidationIssue> issues = new();
        private readonly HashSet<int> contextIds = new();
        private bool hasNullContext;

        public ValidationIssueGroup(ValidationIssue first)
        {
            Severity = first.Severity;
            Code = first.Code;
            Message = first.Message;
            Add(first);
        }

        public ValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public IReadOnlyList<ValidationIssue> Issues => issues;
        public IEnumerable<Object> Contexts => issues.Select(issue => issue.Context).Where(context => context != null);
        public int TargetCount => issues.Count;

        public void Add(ValidationIssue issue)
        {
            if (issue.Context == null)
            {
                if (hasNullContext)
                {
                    return;
                }

                hasNullContext = true;
                issues.Add(issue);
                return;
            }

            if (contextIds.Add(issue.Context.GetInstanceID()))
            {
                issues.Add(issue);
            }
        }
    }

    internal static class ValidationIssueGrouping
    {
        public static IReadOnlyList<ValidationIssueGroup> Group(IEnumerable<ValidationIssue> issues)
        {
            var groups = new List<ValidationIssueGroup>();
            var byKey = new Dictionary<(ValidationSeverity Severity, string Code, string Message), ValidationIssueGroup>();
            foreach (var issue in issues ?? Enumerable.Empty<ValidationIssue>())
            {
                if (issue == null)
                {
                    continue;
                }

                var key = (issue.Severity, issue.Code ?? "", issue.Message ?? "");
                if (!byKey.TryGetValue(key, out var group))
                {
                    group = new ValidationIssueGroup(issue);
                    byKey.Add(key, group);
                    groups.Add(group);
                }
                else
                {
                    group.Add(issue);
                }
            }

            return groups;
        }
    }
}
