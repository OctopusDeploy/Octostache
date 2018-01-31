using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Markdig;

namespace Octostache.Templates.Functions
{
    internal class TextEscapeFunction
    {

        public static string HtmlEscape(string argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, HtmlEntityMap);
        }

        public static string XmlEscape(string argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, XmlEntityMap);
        }

        public static string JsonEscape(string argument, string[] options)
        {
            if (options.Any())
                return null;

            return Escape(argument, JsonEntityMap);
        }

        public static string Markdown(string argument, string[] options)
        {
            if (argument == null || options.Any())
                return null;

            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseEmphasisExtras() //strike through, subscript, superscript
                .UseAutoLinks()      //make links for http:// etc
                .Build();
            return Markdig.Markdown.ToHtml(argument.Trim(), pipeline) + '\n';
        }

        static string Escape(string raw, IDictionary<char, string> entities)
        {
            if (raw == null)
                return null;

            return string.Join("", raw.Select(c =>
            {
                string entity;
                if (entities.TryGetValue(c, out entity))
                    return entity;
                return c.ToString();
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
            { '\"', @"\""" },
            { '\r', @"\r" },
            { '\t', @"\t" },
            { '\n', @"\n" },
            { '\\', @"\\" }
        };
    }
}