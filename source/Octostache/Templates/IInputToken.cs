using Sprache;

namespace Octostache.Templates
{
    interface IInputToken
    {
        Position InputPosition { get; set; }
    }
}