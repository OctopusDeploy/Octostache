namespace Octostache.Templates.Functions
{
    internal static class TextReplaceFunction
    {
        public static string Replace(string argument, string[] options)
        {
            if (argument == null || options.Length == 0 || options.Length > 2)
                return null;

            return argument.Replace(options[0], options.Length == 1 ? "" : options[1]);
        }
    }
}