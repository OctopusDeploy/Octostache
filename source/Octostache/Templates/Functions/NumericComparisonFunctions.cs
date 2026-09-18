using System;
using System.Globalization;

namespace Octostache.Templates.Functions
{
    static class NumericComparisonFunctions
    {
        public static string? LessThan(string? argument, string[] options)
            => Compare(argument, options, (left, right) => left < right);

        public static string? LessThanOrEqual(string? argument, string[] options)
            => Compare(argument, options, (left, right) => left <= right);

        public static string? GreaterThan(string? argument, string[] options)
            => Compare(argument, options, (left, right) => left > right);

        public static string? GreaterThanOrEqual(string? argument, string[] options)
            => Compare(argument, options, (left, right) => left >= right);

        static string? Compare(string? argument, string[] options, Func<double, double, bool> comparer)
        {
            if (argument == null || options.Length != 1)
                return null;

            if (!TryParseNumber(argument, out var left) || !TryParseNumber(options[0], out var right))
                return null;

            return comparer(left, right).ToString().ToLowerInvariant();
        }

        // Both sides are parsed with the invariant culture so a template gives the same answer wherever it is
        // evaluated, rather than depending on the culture of the machine running the deployment. NumberStyles.Float
        // deliberately excludes thousands separators, so an ambiguous value like "1,000" is treated as non-numeric
        // instead of being silently read as 1.
        static bool TryParseNumber(string value, out double result)
            => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
