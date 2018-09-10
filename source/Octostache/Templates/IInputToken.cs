using System.Collections.Generic;
using Sprache;

namespace Octostache.Templates
{
    interface IInputToken
    {
        Position InputPosition { get; set; }

        IEnumerable<string> GetArguments();
    }
}