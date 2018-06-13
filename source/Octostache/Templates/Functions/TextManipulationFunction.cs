using System;
using System.Linq;
using System.Text;

namespace Octostache.Templates.Functions
{
    internal class TextManipulationFunction
    {
        public static string ToBase64(string argument, string[] options)
        {
            if (options.Any() || argument == null)
            {
                return null;
            }
            var argumentBytes = Encoding.UTF8.GetBytes(argument);
            return Convert.ToBase64String(argumentBytes);
        }
    }
}