using System;
using System.Linq;

namespace Octostache.Templates.Functions
{
    static class DateFunction
    {
        public static string? NowDate(string? argument, string[] options)
        {
            if (argument != null || options.Length > 1)
                return null;

            return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).ToString(options.Any() ? options[0] : "O");
        }

        public static string? NowDateUtc(string? argument, string[] options)
        {
            if (argument != null || options.Length > 1)
                return null;

            return DateTime.UtcNow.ToString(options.Any() ? options[0] : "O");
        }
    }
}