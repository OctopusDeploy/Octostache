using System;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class TextToken : TemplateToken
    {
        public TextToken(params string[] text)
        {
            Text = text;
        }

        public IEnumerable<string> Text { get; }

        public override string ToString()
        {
            return string.Concat(Text).Replace("#{", "##{");
        }

        public override IEnumerable<string> GetArguments()
        {
            return new string[0];
        }
    }
}