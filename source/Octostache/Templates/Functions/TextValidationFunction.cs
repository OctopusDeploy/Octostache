using System.Text.RegularExpressions;

namespace Octostache.Templates.Functions
{
    internal static class TextValidationFunction
    {
        public static string? Match(string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            return Regex.Match(argument, options[0]).Success.ToString().ToLowerInvariant();
        }
        
        public static string? StartsWith(string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            return argument.StartsWith(options[0]).ToString().ToLowerInvariant();
        }
        
        public static string? EndsWith(string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            return argument.EndsWith(options[0]).ToString().ToLowerInvariant();
        }
        
        public static string? Contains(string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            return argument.Contains(options[0]).ToString().ToLowerInvariant();
        }
    }
}