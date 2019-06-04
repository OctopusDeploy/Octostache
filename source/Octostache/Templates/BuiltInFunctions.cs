using System;
using System.Collections.Generic;
using Octostache.Templates.Functions;

namespace Octostache.Templates
{
    static class BuiltInFunctions
    {
        static readonly IDictionary<string, Func<string, string[], string>> extensions = new Dictionary<string, Func<string, string[], string>>(StringComparer.OrdinalIgnoreCase)
        {
            {"tolower", TextCaseFunction.ToLower },
            {"toupper", TextCaseFunction.ToUpper },
            {"tobase64", TextManipulationFunction.ToBase64 },
            {"frombase64", TextManipulationFunction.FromBase64 },
            {"htmlescape", TextEscapeFunction.HtmlEscape },
            {"xmlescape", TextEscapeFunction.XmlEscape },
            {"jsonescape", TextEscapeFunction.JsonEscape },
            {"markdown", TextEscapeFunction.MarkdownToHtml },
            {"markdowntohtml", TextEscapeFunction.MarkdownToHtml },
            {"nowdate", DateFunction.NowDate },
            {"nowdateutc", DateFunction.NowDateUtc },
            {"format", FormatFunction.Format },
            {"replace", TextReplaceFunction.Replace },
            {"substring", TextSubstringFunction.Substring},
            {"truncate", TextManipulationFunction.Truncate},
            {"trim", TextManipulationFunction.Trim}
        };

        // Configuration should be done at startup, this isn't thread-safe.
        public static void Register(string name, Func<string, string[], string> implementation)
        {
            var functionName = name.ToLowerInvariant();

            if(!extensions.ContainsKey(functionName))
                extensions.Add(functionName, implementation);
        }

        public static string InvokeOrNull(string function, string argument, string[] options)
        {
            var functionName = function.ToLowerInvariant();

            Func<string, string[], string> ext;
            if (extensions.TryGetValue(functionName, out ext))
                return ext(argument, options);

            return null; // Undefined, will cause source text to print
        }
    }
}