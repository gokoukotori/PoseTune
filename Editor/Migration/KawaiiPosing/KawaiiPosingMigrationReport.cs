using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class KawaiiMigrationReport
    {
        public List<KawaiiMigrationIssue> Issues = new();
        public List<KawaiiMigrationCreatedObject> CreatedObjects = new();
        public int SourceSystemCount;
        public int CreatedGroupCount;
        public int CreatedPoseCount;
        public int SkippedPoseCount;
        public int BlendTreePoseCount;
        public int FootHeightEnabledPoseCount;

        public bool HasErrors => Issues.Exists(issue => issue.Severity == KawaiiMigrationSeverity.Error);

        public void Info(string code, string message, Object context = null)
        {
            Issues.Add(new KawaiiMigrationIssue(KawaiiMigrationSeverity.Info, code, message, context));
        }

        public void Warning(string code, string message, Object context = null)
        {
            Issues.Add(new KawaiiMigrationIssue(KawaiiMigrationSeverity.Warning, code, message, context));
        }

        public void Error(string code, string message, Object context = null)
        {
            Issues.Add(new KawaiiMigrationIssue(KawaiiMigrationSeverity.Error, code, message, context));
        }

        public void Created(Object obj, string kind)
        {
            if (obj == null)
            {
                return;
            }

            CreatedObjects.Add(new KawaiiMigrationCreatedObject
            {
                Object = obj,
                Kind = kind
            });
        }
    }

    internal sealed class KawaiiMigrationIssue
    {
        public readonly KawaiiMigrationSeverity Severity;
        public readonly string Code;
        public readonly string Message;
        public readonly Object Context;

        public KawaiiMigrationIssue(KawaiiMigrationSeverity severity, string code, string message, Object context)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Context = context;
        }
    }

    internal sealed class KawaiiMigrationCreatedObject
    {
        public Object Object;
        public string Kind = "";
    }

    internal enum KawaiiMigrationSeverity
    {
        Info,
        Warning,
        Error
    }
}
