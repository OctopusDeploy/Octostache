using System;
using System.Globalization;
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

        public static string? AddHours(string? argument, string[] options) => AddTimeSpan(TimeSpan.FromHours, argument, options);

        public static string? AddDays(string? argument, string[] options) => AddTimeSpan(TimeSpan.FromDays, argument, options);

        static string? AddTimeSpan(Func<double, TimeSpan> toTimeSpan, string? argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            if (!double.TryParse(options[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                return null;

            try
            {
                var offset = toTimeSpan(amount);

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
                // Amount or result out of range
            }

            return null;
        }
    }
}
