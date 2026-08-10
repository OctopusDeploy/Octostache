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

        public static string? AddSeconds(string? argument, string[] options) => AddDuration(TimeSpan.FromSeconds, argument, options);

        public static string? AddMinutes(string? argument, string[] options) => AddDuration(TimeSpan.FromMinutes, argument, options);

        public static string? AddHours(string? argument, string[] options) => AddDuration(TimeSpan.FromHours, argument, options);

        public static string? AddDays(string? argument, string[] options) => AddDuration(TimeSpan.FromDays, argument, options);

        public static string? AddWeeks(string? argument, string[] options) => AddDuration(weeks => TimeSpan.FromDays(weeks * 7), argument, options);

        public static string? AddTimeSpan(string? argument, string[] options)
        {
            if (options.Length != 1 || !TimeSpan.TryParse(options[0], CultureInfo.InvariantCulture, out var offset))
                return null;

            return Shift(argument, date => date + offset, dateOffset => dateOffset + offset);
        }

        // Calendar months vary in length, so this shifts by month rather than by a fixed duration.
        // Whole months only, and the day is clamped: 31 Jan plus one month is 28 Feb.
        public static string? AddMonths(string? argument, string[] options)
        {
            if (options.Length != 1 || !int.TryParse(options[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var months))
                return null;

            return Shift(argument, date => date.AddMonths(months), dateOffset => dateOffset.AddMonths(months));
        }

        static string? AddDuration(Func<double, TimeSpan> toTimeSpan, string? argument, string[] options)
        {
            if (options.Length != 1 || !double.TryParse(options[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                return null;

            TimeSpan offset;
            try
            {
                offset = toTimeSpan(amount);
            }
            catch
            {
                // Amount out of range
                return null;
            }

            return Shift(argument, date => date + offset, dateOffset => dateOffset + offset);
        }

        static string? Shift(string? argument, Func<DateTime, DateTime> shiftDate, Func<DateTimeOffset, DateTimeOffset> shiftDateOffset)
        {
            if (argument == null)
                return null;

            try
            {
                // Naive stays naive, UTC keeps its 'Z'
                if (DateTime.TryParse(argument, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var date)
                    && date.Kind != DateTimeKind.Local)
                    return shiftDate(date).ToString("O");

                // An explicit offset is kept, not shifted to server-local
                if (DateTimeOffset.TryParse(argument, out var dateOffset))
                    return shiftDateOffset(dateOffset).ToString("O");
            }
            catch
            {
                // Result out of range
            }

            return null;
        }
    }
}
