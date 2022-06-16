using System;
using System.Collections.Generic;
using Octostache.Templates.Functions;

namespace Octostache.Templates
{
    static class BuiltInFunctions
    {
        static readonly IDictionary<string, Func<string?, string[], string?>> Extensions = new Dictionary<string, Func<string?, string[], string?>>(StringComparer.OrdinalIgnoreCase)
        {
            { "tolower", TextCaseFunction.ToLower },
            { "toupper", TextCaseFunction.ToUpper },
            { "tobase64", TextManipulationFunction.ToBase64 },
            { "frombase64", TextManipulationFunction.FromBase64 },
            { "htmlescape", TextEscapeFunctions.HtmlEscape },
            { "uriescape", TextEscapeFunctions.UriStringEscape },
            { "uridataescape", TextEscapeFunctions.UriDataStringEscape },
            { "xmlescape", TextEscapeFunctions.XmlEscape },
            { "jsonescape", TextEscapeFunctions.JsonEscape },
            { "yamlsinglequoteescape", TextEscapeFunctions.YamlSingleQuoteEscape },
            { "yamldoublequoteescape", TextEscapeFunctions.YamlDoubleQuoteEscape },
            { "propertieskeyescape", TextEscapeFunctions.PropertiesKeyEscape },
            { "propertiesvalueescape", TextEscapeFunctions.PropertiesValueEscape },
            { "markdown", TextEscapeFunctions.MarkdownToHtml },
            { "markdowntohtml", TextEscapeFunctions.MarkdownToHtml },
            { "nowdate", DateFunction.NowDate },
            { "nowdateutc", DateFunction.NowDateUtc },
            { "format", FormatFunction.Format },
            { "match", TextComparisonFunctions.Match },
            { "replace", TextReplaceFunction.Replace },
            { "startswith", TextComparisonFunctions.StartsWith },
            { "endswith", TextComparisonFunctions.EndsWith },
            { "contains", TextComparisonFunctions.Contains },
            { "substring", TextSubstringFunction.Substring },
            { "truncate", TextManipulationFunction.Truncate },
            { "trim", TextManipulationFunction.Trim },
            { "indent", TextManipulationFunction.Indent },
            { "uripart", TextManipulationFunction.UriPart },
            { "versionmajor", VersionParseFunction.VersionMajor },
            { "versionminor", VersionParseFunction.VersionMinor },
            { "versionpatch", VersionParseFunction.VersionPatch },
            { "versionprerelease", VersionParseFunction.VersionRelease },
            { "versionprereleaseprefix", VersionParseFunction.VersionReleasePrefix },
            { "versionprereleasecounter", VersionParseFunction.VersionReleaseCounter },
            { "versionrevision", VersionParseFunction.VersionRevision },
            { "versionmetadata", VersionParseFunction.VersionMetadata },
            { "append", TextManipulationFunction.Append },
            { "prepend", TextManipulationFunction.Prepend },
            { "md5", HashFunction.Md5 },
            { "sha1", HashFunction.Sha1 },
            { "sha256", HashFunction.Sha256 },
            { "sha384", HashFunction.Sha384 },
            { "sha512", HashFunction.Sha512 },
        };

        public static string? InvokeOrNull(string function, string? argument, string[] options)
        {
            var functionName = function.ToLowerInvariant();

            if (Extensions.TryGetValue(functionName, out var ext))
                return ext(argument, options);

            return null; // Undefined, will cause source text to print
        }
    }
}
