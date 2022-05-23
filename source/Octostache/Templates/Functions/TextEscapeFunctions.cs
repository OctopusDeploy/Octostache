using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;

namespace Octostache.Templates.Functions
{
    class TextEscapeFunctions
    {
        static readonly Regex NewLineRegex = new Regex(@"(?:\r?\n)+", RegexOptions.Compiled);

        static readonly IDictionary<char, string> HtmlEntityMap = new Dictionary<char, string>
        {
            { '&', "&amp;" },
            { '<', "&lt;" },
            { '>', "&gt;" },
            { '"', "&quot;" },
            { '\'', "&apos;" },
            { '/', "&#x2F;" }
        };

        static readonly IDictionary<char, string> XmlEntityMap = new Dictionary<char, string>
        {
            { '&', "&amp;" },
            { '<', "&lt;" },
            { '>', "&gt;" },
            { '"', "&quot;" },
            { '\'', "&apos;" }
        };

        // This is overly simplistic since Unicode chars also need escaping.
        static readonly IDictionary<char, string> JsonEntityMap = new Dictionary<char, string>
        {
            { '\"', @"\""" },
            { '\r', @"\r" },
            { '\t', @"\t" },
            { '\n', @"\n" },
            { '\\', @"\\" }
        };

        static readonly IDictionary<char, string> YamlSingleQuoteMap = new Dictionary<char, string>
        {
            { '\'', "''" }
        };

        public static string? HtmlEscape(string? argument, string[] options)
        {
            return options.Any() ? null : Escape(argument, HtmlEntityMap);
        }

        public static string? XmlEscape(string? argument, string[] options)
        {
            return options.Any() ? null : Escape(argument, XmlEntityMap);
        }

        public static string? JsonEscape(string? argument, string[] options)
        {
            return options.Any() ? null : Escape(argument, JsonEntityMap);
        }

        public static string? YamlSingleQuoteEscape(string? argument, string[] options)
        {
            // https://yaml.org/spec/history/2002-10-31.html#syntax-single

            if (argument == null || options.Any())
                return null;

            argument = HandleSingleQuoteYamlNewLines(argument);

            return Escape(argument, YamlSingleQuoteMap);
        }

        public static string? YamlDoubleQuoteEscape(string? argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, YamlDoubleQuoteMap);
        }

        static string HandleSingleQuoteYamlNewLines(string input)
        {
            // A single newline is parsed by YAML as a space
            // A double newline is parsed by YAML as a single newline
            // A triple newline is parsed by YAML as a double newline
            // ...etc

            var output = NewLineRegex.Replace(input,
                                              m =>
                                              {
                                                  var newlineToInsert = m.Value.StartsWith("\r")
                                                      ? "\r\n"
                                                      : "\n";

                                                  return newlineToInsert + m.Value;
                                              });

            return output;
        }

        public static string? PropertiesKeyEscape(string? argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, PropertiesKeyMap);
        }

        public static string? PropertiesValueEscape(string? argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, PropertiesValueMap);
        }

        public static string? MarkdownToHtml(string? argument, string[] options)
        {
            if (argument == null || options.Any())
                return null;

            var pipeline = new MarkdownPipelineBuilder()
                           .UsePipeTables()
                           .UseEmphasisExtras() //strike through, subscript, superscript
                           .UseAutoLinks() //make links for http:// etc
                           .Build();
            return Markdown.ToHtml(argument.Trim(), pipeline) + '\n';
        }

        public static string? UriStringEscape(string? argument, string[] options)
        {
            if (options.Any())
                return null;

            if (argument == null)
                return null;

            return Uri.EscapeUriString(argument);
        }

        public static string? UriDataStringEscape(string? argument, string[] options)
        {
            if (options.Any())
                return null;

            if (argument == null)
                return null;

            return Uri.EscapeDataString(argument);
        }

        [return: NotNullIfNotNull("raw")]
        static string? Escape(string? raw, IDictionary<char, string> entities)
        {
            if (raw == null)
                return null;

            return string.Join("",
                               raw.Select(c =>
                                          {
                                              string entity;
                                              if (entities.TryGetValue(c, out entity))
                                                  return entity;
                                              return c.ToString();
                                          }));
        }

        [return: NotNullIfNotNull("raw")]
        static string? Escape(string? raw, Func<char, string> mapping)
        {
            return raw == null ? null : string.Join("", raw.Select(mapping));
        }

        [return: NotNullIfNotNull("raw")]
        static string? Escape(string? raw, Func<char, int, string> mapping)
        {
            return raw == null ? null : string.Join("", raw.Select(mapping));
        }

        static bool IsAsciiPrintable(char ch)
        {
            return ch >= 0x20 && ch <= 0x7E;
        }

        static bool IsIso88591Compatible(char ch)
        {
            return ch >= 0x00 && ch < 0xFF;
        }

        static string EscapeUnicodeCharForYamlOrProperties(char ch)
        {
            var hex = ((int)ch).ToString("x4");
            return $"\\u{hex}";
        }

        static string YamlDoubleQuoteMap(char ch)
        {
            // Yaml supports multiple ways to encode newlines. One method we tried
            // (doubling newlines) doesn't work consistently across all libraries/
            // validators, so we've gone with escaping newlines (\\r, \\n) instead.

            switch (ch)
            {
                case '\r':
                    return "\\r";
                case '\n':
                    return "\\n";
                case '\t':
                    return "\\t";
                case '\\':
                    return "\\\\";
                case '"':
                    return "\\\"";
                default:
                    return IsAsciiPrintable(ch) ? ch.ToString() : EscapeUnicodeCharForYamlOrProperties(ch);
            }
        }

        static string CommonPropertiesMap(char ch)
        {
            switch (ch)
            {
                case '\\':
                    return "\\\\";
                case '\r':
                    return "\\r";
                case '\n':
                    return "\\n";
                case '\t':
                    // In some contexts a tab can get treated as non-semantic whitespace,
                    // or as part of the separator between keys and values. It's safer to
                    // always encode tabs.
                    return "\\t";
                default:
                    return IsIso88591Compatible(ch)
                        ? ch.ToString()
                        : EscapeUnicodeCharForYamlOrProperties(ch);
            }
        }

        static string PropertiesKeyMap(char ch)
        {
            switch (ch)
            {
                case ' ':
                    return "\\ ";
                case ':':
                    return "\\:";
                case '=':
                    return "\\=";
                default:
                    return CommonPropertiesMap(ch);
            }
        }

        static string PropertiesValueMap(char ch, int index)
        {
            switch (ch)
            {
                case ' ' when index == 0:
                    return "\\ ";
                default:
                    return CommonPropertiesMap(ch);
            }
        }
    }
}