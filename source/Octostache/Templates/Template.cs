using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    public class Template
    {
        public TemplateToken[] Tokens { get; }

        public Template(IEnumerable<TemplateToken> tokens)
        {
            Tokens = tokens.ToArray();
        }

        public override string ToString() => string.Concat(Tokens.Cast<object>());
    }
}
