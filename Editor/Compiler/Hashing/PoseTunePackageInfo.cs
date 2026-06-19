using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor.Compiler.Hashing
{
    internal static class PoseTunePackageInfo
    {
        public static string Version
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PoseTuneRoot).Assembly);
                return string.IsNullOrWhiteSpace(package?.version) ? "unknown" : package.version;
            }
        }
    }
}
