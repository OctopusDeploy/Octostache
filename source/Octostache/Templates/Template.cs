using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class Template
    {
        readonly TemplateToken[] tokens;

        public Template(IEnumerable<TemplateToken> tokens)
        {
            this.tokens = tokens.ToArray();
        }

        public TemplateToken[] Tokens
        {
            get { return tokens; }
        }

        public override string ToString()
        {
            return string.Concat(tokens.Cast<object>());
        }
    }
}
