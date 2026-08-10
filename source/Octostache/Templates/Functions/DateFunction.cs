using System;
using System.Globalization;
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

        public static string? AddTimeSpan(string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            if (!TimeSpan.TryParse(options[0], CultureInfo.InvariantCulture, out var offset))
                return null;

            try
            {
                // Naive stays naive, UTC keeps its 'Z'
                if (DateTime.TryParse(argument, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var date)
                    && date.Kind != DateTimeKind.Local)
                    return (date + offset).ToString("O");

                // An explicit offset is kept, not shifted to server-local
                if (DateTimeOffset.TryParse(argument, out var dateOffset))
                    return (dateOffset + offset).ToString("O");
            }
            catch
            {
                // Result out of range
            }

            return null;
        }
    }
}
