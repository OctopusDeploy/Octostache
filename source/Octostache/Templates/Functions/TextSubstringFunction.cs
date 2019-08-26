using System.Linq;

namespace Octostache.Templates.Functions
{
    internal static class TextSubstringFunction
    {
        public static string Substring(string argument, string[] options)
        {
            if (argument == null ||
                options.Length == 0 ||
                options.Length > 2 ||
                options.Any(o => !int.TryParse(o, out int _)) ||
                options.Any(o => int.Parse(o) < 0) ||
                (options.Length == 1 && int.Parse(options[0]) > argument.Length) ||
                (options.Length == 2 && (int.Parse(options[0]) + int.Parse(options[1]) > argument.Length)))
                return null;

            return argument.Substring(
                options.Length == 1 ? 0 : int.Parse(options[0]),
                options.Length == 1 ? int.Parse(options[0]) : int.Parse(options[1]));
        }
    }
}