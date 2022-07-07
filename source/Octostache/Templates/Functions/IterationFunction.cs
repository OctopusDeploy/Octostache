using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates.Functions
{
    class IterationFunction
    {
        public static IEnumerable<Binding>? Reverse(IEnumerable<Binding>? argument, string[] options) => options.Any() ? null : argument?.Reverse();
    }
}
