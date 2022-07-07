using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class RepetitionToken : TemplateToken
    {
        public ContentExpression Collection { get; }
        public Identifier Enumerator { get; }
        public TemplateToken[] Template { get; }

        public RepetitionToken(ContentExpression collection, Identifier enumerator, IEnumerable<TemplateToken> template)
        {
            Collection = collection;
            Enumerator = enumerator;
            Template = template.ToArray();
        }

        public override string ToString()
            => "#{each " + Enumerator + " in " + Collection + "}" + string.Join("", Template.Cast<object>()) + "#{/each}";

        public override IEnumerable<string> GetArguments() => Collection.GetArguments();
    }
}
