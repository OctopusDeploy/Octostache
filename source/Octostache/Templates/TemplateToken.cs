using System.Collections.Generic;
using Sprache;

namespace Octostache.Templates
{
    public abstract class TemplateToken : IInputToken
    {
        public Position? InputPosition { get; set; }
        public abstract IEnumerable<string> GetArguments();
    }
}