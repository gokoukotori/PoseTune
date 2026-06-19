namespace Gokoukotori.PoseTune
{
    internal static class ObjectNamesFallback
    {
        public static string Nicify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value.Replace('_', ' ').Replace('-', ' ');
        }
    }
}
