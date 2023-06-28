using System;

namespace Octostache.Templates.Functions
{
    static class NullFunction
    {
        public static string? Null(string? argument, string[] options)
        {
            return null;
        }
    }
}
