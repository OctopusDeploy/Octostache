using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class RepetitionToken : TemplateToken
    {
        public SymbolExpression Collection { get; }
        public Identifier Enumerator { get; }
        public TemplateToken[] Template { get; }
        public bool Reversed { get; }

        public RepetitionToken(SymbolExpression collection, Identifier enumerator, IEnumerable<TemplateToken> template, string sorting)
        {
            Collection = collection;
            Enumerator = enumerator;
            Template = template.ToArray();
            Reversed = sorting == "reversed";
        }

        public override string ToString()
            => "#{each " + Enumerator + " in " + Collection + "}" + string.Join("", Template.Cast<object>()) + "#{/each}";

        public override IEnumerable<string> GetArguments() => Collection.GetArguments();
    }
}
