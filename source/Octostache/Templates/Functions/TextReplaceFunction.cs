using System;
using System.Text.RegularExpressions;

namespace Octostache.Templates.Functions
{
    static class TextReplaceFunction
    {
        public static string? Replace(string? argument, string[] options)
        {
            if (argument == null || options.Length == 0)
                return null;

            return Regex.Replace(argument, options[0], options.Length == 1 ? "" : options[1]);
        }
    }
}