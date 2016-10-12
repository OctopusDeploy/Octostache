using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MarkdownSharp;

namespace Octostache.Templates
{
    static class BuiltInFunctions
    {
        static readonly IDictionary<string, Func<string[], string>> extensions = new Dictionary<string, Func<string[], string>>(StringComparer.OrdinalIgnoreCase);
 
        // Configuration shoudl be done at startup, this isn't thread-safe.
        public static void Register(string name, Func<string[], string> implementation)
        {
            extensions.Add(name.ToLowerInvariant(), implementation);
        }

        public static string InvokeOrNull(string function, string[] args)
        {
            var functionName = function.ToLowerInvariant();

            switch (functionName)
            {
                case "tolower":
                    return args[0].ToLower(); // Happy to leave this culture-specific
                case "toupper":
                    return args[0].ToUpper();
                case "htmlescape":
                    return HtmlEscape(args[0]);
                case "xmlescape":
                    return XmlEscape(args[0]);
                case "jsonescape":
                    return JsonEscape(args[0]);
                case "markdown":
                    return Markdown(args[0]);
                case "formatdate":
                    return DateTimeFormat(args[0], args[1]);
            }

            Func<string[], string> ext;
            if (extensions.TryGetValue(functionName, out ext))
                return ext(args);

            return null; // Undefined, will cause source text to print
        }

        static string DateTimeFormat(string raw, string format)
        {
            DateTime val;
            if (DateTime.TryParse(raw, out val))
            {
                try
                {
                    return val.ToString(format);
                }
                catch (FormatException)
                {
                }
            }
            return raw;
        }

        static string HtmlEscape(string raw)
        {
            return Escape(raw, HtmlEntityMap);
        }

        static string XmlEscape(string raw)
        {
            return Escape(raw, XmlEntityMap);
        }

        static string JsonEscape(string raw)
        {
            return Escape(raw, JsonEntityMap);
        }

        static string Markdown(string raw)
        {
            var options = new MarkdownOptions
            {
                AutoHyperlink = true,
                LinkEmails = true
            };

            return new Markdown(options).Transform(raw.Trim());
        }

        static string Escape(string raw, IDictionary<char, string> entities)
        {
            return string.Join("", raw.Select(c =>
            {
                string entity;
                if (entities.TryGetValue(c, out entity))
                    return entity;
                return c.ToString(CultureInfo.InvariantCulture);
            }));
        }

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
                                                                    { '\"', "\\\"" },
                                                                    { '\r', "\\\r" },
                                                                    { '\t', "\\\t" },
                                                                    { '\n', "\\\n" },
                                                                    { '\\', "\\\\" }
                                                                };
    }
}