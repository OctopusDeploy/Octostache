using System;

namespace Octostache.Templates.Functions
{
    /// <summary>
    /// Supplies the match timeout used by the filters that run a caller-supplied
    /// regular expression (<see cref="TextReplaceFunction" /> and <see cref="TextComparisonFunctions" />).
    /// </summary>
    static class RegexDefaults
    {
        const string AppDomainKey = "REGEX_DEFAULT_MATCH_TIMEOUT";

        static readonly TimeSpan Fallback = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Defers to the timeout the host has configured for the process, falling back to a
        /// generous default so that a pattern which backtracks heavily cannot run indefinitely
        /// in a host that has not configured one.
        /// </summary>
        /// <remarks>
        /// Resolved per call rather than cached, so that a host setting the value during its own
        /// start-up is not raced by the first evaluation.
        /// </remarks>
        public static TimeSpan MatchTimeout
            => AppDomain.CurrentDomain.GetData(AppDomainKey) is TimeSpan hostTimeout
                ? hostTimeout
                : Fallback;
    }
}
