using System;
using System.Linq;

namespace Octostache.Templates.Functions
{
    static class TextSubstringFunction
    {
        public static string? Substring(string? argument, string[] options)
        {
            if (argument == null || options.Length == 0 || options.Length > 2)
                return null;

            if (options.Any(o => !int.TryParse(o, out _)) || options.Any(o => int.Parse(o) < 0))
                return null;

            var startIndex = options.Length == 1 ? 0 : int.Parse(options[0]);
            var length = options.Length == 1 ? int.Parse(options[0]) : int.Parse(options[1]);

            // A start position past the end of the string is not a request we can honour
            if (startIndex > argument.Length)
                return null;

            // Asking for more characters than remain yields whatever is left of the string
            if (startIndex + length > argument.Length)
                length = argument.Length - startIndex;

            return argument.Substring(startIndex, length);
        }
    }
}
