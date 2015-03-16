using Sprache;

namespace Octostache.Templates
{
    abstract class TemplateToken : IInputToken
    {
        public Position InputPosition { get; set; }
    }
}