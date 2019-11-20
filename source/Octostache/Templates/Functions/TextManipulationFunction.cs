using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Octostache.Templates.Functions
{
    internal class TextManipulationFunction
    {
        public static string ToBase64(string argument, string[] options)
        {
            if (options.Length > 1 || argument == null)
            {
                return null;
            }

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
        
        public static string FromBase64(string argument, string[] options)
        {
            if (options.Length > 1 || argument == null)
            {
                return null;
            }

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

        public static string Truncate(string argument, string[] options)
        {
            if (argument == null ||
                !options.Any() ||
                !int.TryParse(options[0], out int _) ||
                int.Parse(options[0]) < 0)
                return null;

            var length = int.Parse(options[0]);
            return length < argument.Length
                ? $"{argument.Substring(0, length)}..."
                : argument;
        }

        public static string Trim(string argument, string[] options)
        {
            if (argument == null)
                return null;

            if (!options.Any()) return argument.Trim();

            switch(options[0].ToLower())
            {
                case "start":
                    return argument.TrimStart();
                case "end":
                    return argument.TrimEnd();
                default:
                    return null;
            }
        }

        public static string UriPart(string argument, string[] options)
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
    }
}