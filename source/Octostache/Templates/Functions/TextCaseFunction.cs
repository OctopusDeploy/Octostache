using System;
using System.Linq;

namespace Octostache.Templates.Functions
{
    class TextCaseFunction
    {
        public static string? ToUpper(string? argument, string[] options) => options.Any() ? null : argument?.ToUpper();

        public static string? ToLower(string? argument, string[] options) => options.Any() ? null : argument?.ToLower();
    }
}
