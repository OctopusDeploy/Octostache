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

            if (options.Length == 1 && int.Parse(options[0]) > argument.Length)
                return null;

            if (options.Length == 2 && int.Parse(options[0]) > argument.Length)
                return null;

            var startIndex = options.Length == 1 ? 0 : int.Parse(options[0]);
            var length = options.Length == 1 ? int.Parse(options[0]) : int.Parse(options[1]);

            // If starting position is valid but the length would exceed the string, use the remaining length of the string
            if (startIndex < argument.Length && startIndex + length > argument.Length)
                length = argument.Length - startIndex;

            return argument.Substring(startIndex, length);
        }
    }
}
