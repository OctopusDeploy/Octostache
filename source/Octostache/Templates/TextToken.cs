using System.Collections;
using System.Collections.Generic;

namespace Octostache.Templates
{
    class TextToken : TemplateToken
    {
        readonly IList<string> text;

        public TextToken(IList<string> text)
        {
            this.text = text;
        }

        public IList<string> Text
        {
            get { return text; }
        }

        public override string ToString()
        {
            return string.Concat(text).Replace("#{", "##{");
        }
    }
}