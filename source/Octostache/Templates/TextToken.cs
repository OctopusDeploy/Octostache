using System.Collections.Generic;

namespace Octostache.Templates
{
    class TextToken : TemplateToken
    {
        public IEnumerable<string> Text { get;  }


        public TextToken(params string[] text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return string.Concat(Text).Replace("#{", "##{");
        }
    }
}