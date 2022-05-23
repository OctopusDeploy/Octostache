using System;

namespace Octostache.Templates.Functions
{
    class FormatFunction
    {
        public static string? Format(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            if (options.Length == 0)
                return null;

            var formatString = options[0];

            try
            {
                if (options.Length == 1)
                {
                    decimal dbl;
                    if (decimal.TryParse(argument, out dbl))
                        return dbl.ToString(formatString);

                    DateTimeOffset dto;
                    if (DateTimeOffset.TryParse(argument, out dto))
                        return dto.ToString(formatString);

                    return null;
                }

                if (options.Length != 2)
                    return null;

                formatString = options[1];
                switch (options[0].ToLower())
                {
                    case "int32":
                    case "int":
                        int result;
                        if (int.TryParse(argument, out result))
                            return result.ToString(formatString);
                        break;
                    case "double":
                        double dbl;
                        if (double.TryParse(argument, out dbl))
                            return dbl.ToString(formatString);
                        break;
                    case "decimal":
                        decimal dcml;
                        if (decimal.TryParse(argument, out dcml))
                            return dcml.ToString(formatString);
                        break;
                    case "date":
                    case "datetime":
                        DateTime date;
                        if (DateTime.TryParse(argument, out date))
                            return date.ToString(formatString);
                        break;
                    case "datetimeoffset":
                        DateTimeOffset dateOffset;
                        if (DateTimeOffset.TryParse(argument, out dateOffset))
                            return dateOffset.ToString(formatString);
                        break;
                }
            }
            catch (FormatException) { }

            return null;
        }
    }
}