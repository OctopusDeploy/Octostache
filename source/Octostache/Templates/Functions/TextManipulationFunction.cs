using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Octostache.Templates.Functions
{
    class TextManipulationFunction
    {
        public static string? ToBase64(string? argument, string[] options)
        {
            if (options.Length > 1 || argument == null)
                return null;

            var encoding = !options.Any() ? "utf8" : options[0].ToLower();

            byte[] argumentBytes;
            switch (encoding)
            {
                case "utf8":
                case "utf-8":
                {
                    argumentBytes = Encoding.UTF8.GetBytes(argument);
                    break;
                }
                case "unicode":
                {
                    argumentBytes = Encoding.Unicode.GetBytes(argument);
                    break;
                }
                default:
                {
                    return null;
                }
            }

            return Convert.ToBase64String(argumentBytes);
        }

        public static string? FromBase64(string? argument, string[] options)
        {
            if (options.Length > 1 || argument == null)
                return null;

            var encoding = !options.Any() ? "utf8" : options[0].ToLower();
            var argumentBytes = Convert.FromBase64String(argument);
            switch (encoding)
            {
                case "utf8":
                case "utf-8":
                {
                    return Encoding.UTF8.GetString(argumentBytes);
                }
                case "unicode":
                {
                    return Encoding.Unicode.GetString(argumentBytes);
                }
                default:
                {
                    return null;
                }
            }
        }

        public static string? Append(string? argument, string[] options)
        {
            if (argument == null)
            {
                if (!options.Any())
                    return null;

                return string.Concat(options);
            }

            if (!options.Any())
                return null;

            return argument + string.Concat(options);
        }

        public static string? Prepend(string? argument, string[] options)
        {
            if (argument == null)
            {
                if (!options.Any())
                    return null;

                return string.Concat(options);
            }

            if (!options.Any())
                return null;

            return string.Concat(options) + argument;
        }

        public static string? Truncate(string? argument, string[] options)
        {
            if (argument == null || !options.Any() || !int.TryParse(options[0], out var _) || int.Parse(options[0]) < 0)
                return null;

            var length = int.Parse(options[0]);
            return length < argument.Length
                ? $"{argument.Substring(0, length)}..."
                : argument;
        }

        [return: NotNullIfNotNull("argument")]
        public static string? Trim(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            if (!options.Any()) return argument.Trim();

            switch (options[0].ToLower())
            {
                case "start":
                    return argument.TrimStart();
                case "end":
                    return argument.TrimEnd();
                default:
                    return null;
            }
        }

        public static string? Indent(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            if (argument.Length == 0)
                // No content, no indenting
                return string.Empty;

            var indentOptions = new IndentOptions(options);

            if (!indentOptions.IsValid)
                return null;

            return indentOptions.InitialIndent + argument.Replace("\n", "\n" + indentOptions.SubsequentIndent);
        }

        [return: NotNullIfNotNull("argument")]
        public static string? UriPart(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            if (!options.Any())
                return $"[{nameof(UriPart)} error: no argument given]";

            if (!Uri.TryCreate(argument, UriKind.RelativeOrAbsolute, out var uri))
                return argument;

            // NOTE: IdnHost property not available in .NET Framework target

            try
            {
                switch (options[0].ToLowerInvariant())
                {
                    case "absolutepath":
                        return uri.AbsolutePath;
                    case "absoluteuri":
                        return uri.AbsoluteUri;
                    case "authority":
                        return uri.Authority;
                    case "dnssafehost":
                        return uri.DnsSafeHost;
                    case "fragment":
                        return uri.Fragment;
                    case "host":
                        return uri.Host;
                    case "hostandport":
                        return uri.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);
                    case "hostnametype":
                        return Enum.GetName(typeof(UriHostNameType), uri.HostNameType);
                    case "isabsoluteuri":
                        return uri.IsAbsoluteUri.ToString().ToLowerInvariant();
                    case "isdefaultport":
                        return uri.IsDefaultPort.ToString().ToLowerInvariant();
                    case "isfile":
                        return uri.IsFile.ToString().ToLowerInvariant();
                    case "isloopback":
                        return uri.IsLoopback.ToString().ToLowerInvariant();
                    case "isunc":
                        return uri.IsUnc.ToString().ToLowerInvariant();
                    case "path":
                        return uri.LocalPath;
                    case "pathandquery":
                        return uri.PathAndQuery;
                    case "port":
                        return uri.Port.ToString(CultureInfo.InvariantCulture);
                    case "query":
                        return uri.Query;
                    case "scheme":
                        return uri.Scheme;
                    case "schemeandserver":
                        return uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                    case "userinfo":
                        return uri.UserInfo;
                    default:
                        return $"[{nameof(UriPart)} {options[0]} error: argument '{options[0]}' not supported]";
                }
            }
            catch (Exception e)
            {
                return $"[{nameof(UriPart)} {options[0]} error: {e.Message}]";
            }
        }

        class IndentOptions
        {
            static readonly Regex dualSizeEx = new Regex(@"^((\d{1,3})?/)?(\d{1,3})$", RegexOptions.Compiled);

            public IndentOptions(string[] options)
            {
                if (options.Length == 0)
                {
                    SubsequentIndent = InitialIndent = "    ";
                    return;
                }

                if (options.Length == 1)
                {
                    var dualSize = dualSizeEx.Match(options[0]);
                    if (dualSize.Success)
                    {
                        var separator = dualSize.Groups[1];
                        var initial = dualSize.Groups[2];
                        var subsequent = dualSize.Groups[3];
                        if (separator.Success)
                        {
                            // Different sized indents
                            if (initial.Success)
                            {
                                if (byte.TryParse(initial.Value, out var initialIndent) && byte.TryParse(subsequent.Value, out var subsequentIndent))
                                {
                                    InitialIndent = new string(' ', initialIndent);
                                    SubsequentIndent = new string(' ', subsequentIndent);
                                    return;
                                }
                            }
                            else
                            {
                                if (byte.TryParse(subsequent.Value, out var subsequentIndent))
                                {
                                    InitialIndent = string.Empty;
                                    SubsequentIndent = new string(' ', subsequentIndent);
                                    return;
                                }
                            }
                        }
                        else if (byte.TryParse(subsequent.Value, out var overallIndent))
                        {
                            InitialIndent = SubsequentIndent = new string(' ', overallIndent);
                            return;
                        }
                    }

                    InitialIndent = SubsequentIndent = options[0];
                }
                else if (options.Length == 2)
                {
                    InitialIndent = options[0];
                    SubsequentIndent = options[1];
                }
                else
                {
                    InitialIndent = SubsequentIndent = string.Empty;
                    IsValid = false;
                }
            }

            public string InitialIndent { get; }
            public string SubsequentIndent { get; }
            public bool IsValid { get; } = true;
        }
    }
}