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
    }
}