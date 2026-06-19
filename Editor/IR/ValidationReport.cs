using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class ValidationReport
    {
        public List<ValidationIssue> Errors = new();
        public List<ValidationIssue> Warnings = new();

        public bool HasErrors => Errors.Count > 0;
        public IEnumerable<ValidationIssue> Issues => Errors.Concat(Warnings);

        public void Error(string code, string message, Object context = null)
        {
            Errors.Add(new ValidationIssue(ValidationSeverity.Error, code, message, context));
        }

        public void Warning(string code, string message, Object context = null)
        {
            Warnings.Add(new ValidationIssue(ValidationSeverity.Warning, code, message, context));
        }
    }

    public enum ValidationSeverity
    {
        Error,
        Warning
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
}
