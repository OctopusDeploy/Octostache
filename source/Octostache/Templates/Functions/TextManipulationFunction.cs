using System;
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
    }
}