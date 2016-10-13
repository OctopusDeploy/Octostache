using System;
using System.Linq;
using System.Text;

namespace Octostache.Templates.Functions
{
    internal class DateFunction
    {
        public static string NowDate(string argument, string[] options)
        {
            if (argument != null || options.Length > 1)
                return null;

            return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).ToString(options.Any() ? options[0] : "O");
        }

        public static string NowDateUtc(string argument, string[] options)
        {
            if (argument != null || options.Length > 1)
                return null;

            return DateTime.UtcNow.ToString(options.Any() ? options[0] : "O");
        }

        public static string DateTimeFormat(string argument, string[] options)
        {
            if (argument == null || options.Length != 1)
                return null;

            DateTimeOffset val;
            if (DateTimeOffset.TryParse(argument, out val))
            {
                try
                {
                    return val.ToString(options[0]);
                }
                catch (FormatException)
                {
                }
            }
            return null;
        }
    }
}
