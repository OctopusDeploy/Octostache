using System;
using System.Linq;

namespace Octostache.Templates.Functions
{
    static class DateFunction
    {
        public static string? NowDate(string? argument, string[] options)
        {
            if (argument != null || options.Length > 2)
                return null;

            string? formatString = null;
            TimeZoneInfo? tz = null;

            foreach (var option in options)
            {
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById(option);
                }
                catch (TimeZoneNotFoundException)
                {
                    formatString = option;
                }
            }

            var dt = (tz == null) ? DateTime.Now : TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            return dt.ToString(formatString ?? "O");
        }

        public static string? NowDateUtc(string? argument, string[] options)
        {
            if (argument != null || options.Length > 1)
                return null;

            return DateTime.UtcNow.ToString(options.Any() ? options[0] : "O");
        }
    }
}
