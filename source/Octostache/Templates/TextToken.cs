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

        public IEnumerable<string> Text
        {
            get { return text; }
        }

        public override string ToString()
        {
            return string.Concat(text).Replace("#{", "##{");
        }
    }
}