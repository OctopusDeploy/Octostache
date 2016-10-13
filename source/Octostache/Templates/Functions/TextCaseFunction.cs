using System.Linq;

namespace Octostache.Templates.Functions
{
    internal class TextCaseFunction
    {
        public static string ToUpper(string argument, string[] options)
        {
            return options.Any() ? null : argument?.ToUpper();
        }

        public static string ToLower(string argument, string[] options)
        {
            return options.Any() ? null : argument?.ToLower();
        }
    }
}