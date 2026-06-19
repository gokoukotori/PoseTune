using UnityEngine;

namespace Gokoukotori.PoseTune
{
    public static class PoseTuneLog
    {
        private const string Prefix = "[PoseTune] ";

        public static void Info(string message, Object context = null)
        {
            Debug.Log(Prefix + message, context);
        }

        public static void Warning(string message, Object context = null)
        {
            Debug.LogWarning(Prefix + message, context);
        }

        public static void Error(string message, Object context = null)
        {
            Debug.LogError(Prefix + message, context);
        }
    }
}
