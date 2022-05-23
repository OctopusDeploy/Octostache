using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    public class Template
    {
        public Template(IEnumerable<TemplateToken> tokens)
        {
            Tokens = tokens.ToArray();
        }

        public TemplateToken[] Tokens { get; }

        public override string ToString()
        {
            return string.Concat(Tokens.Cast<object>());
        }
    }
}