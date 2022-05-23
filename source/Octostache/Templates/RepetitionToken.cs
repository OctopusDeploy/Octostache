using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class RepetitionToken : TemplateToken
    {
        public RepetitionToken(SymbolExpression collection, Identifier enumerator, IEnumerable<TemplateToken> template)
        {
            Collection = collection;
            Enumerator = enumerator;
            Template = template.ToArray();
        }

        public SymbolExpression Collection { get; }

        public Identifier Enumerator { get; }

        public TemplateToken[] Template { get; }

        public override string ToString()
        {
            return "#{each " + Enumerator + " in " + Collection + "}" + string.Join("", Template.Cast<object>()) + "#{/each}";
        }

        public override IEnumerable<string> GetArguments()
        {
            return Collection.GetArguments();
        }
    }
}