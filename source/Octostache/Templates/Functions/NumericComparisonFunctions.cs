using System;
using System.Globalization;
using System.Runtime.CompilerServices;

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

        // The caller is captured so that Error.Format reports the filter the author actually wrote
        // (`[LessThan error: ...]`) rather than the name of this helper.
        static string? Compare(string? argument, string[] options, Func<double, double, bool> comparer, [CallerMemberName] string? caller = null)
        {
            // Being given the wrong number of arguments is a mistake in the template itself, which no value could
            // make valid, so it is reported as an error rather than silently comparing as false.
            if (options.Length != 1)
                return Error.Format(options.Length == 0 ? "no argument given" : $"expected 1 argument, got {options.Length}", caller: caller);

            // An unresolved variable is left to the missing token machinery, consistent with every other filter, so
            // that a misspelled variable name is reported instead of quietly comparing as false.
            if (argument == null)
                return null;

            // A value that is present but not a number cannot satisfy any comparison, so it compares as false.
            if (!TryParseNumber(argument, out var left) || !TryParseNumber(options[0], out var right))
                return bool.FalseString.ToLowerInvariant();

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
