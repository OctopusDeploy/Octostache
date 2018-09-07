using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    class RepetitionToken : TemplateToken
    {
        readonly SymbolExpression collection;
        readonly Identifier enumerator;
        readonly TemplateToken[] template;

        public RepetitionToken(SymbolExpression collection, Identifier enumerator, IEnumerable<TemplateToken> template)
        {
            this.collection = collection;
            this.enumerator = enumerator;
            this.template = template.ToArray();
        }

        public SymbolExpression Collection
        {
            get { return collection; }
        }

        public Identifier Enumerator
        {
            get { return enumerator; }
        }

        public TemplateToken[] Template
        {
            get { return template; }
        }

        public override string ToString()
        {
            return "#{each " + Enumerator + " in " + Collection + "}" + string.Join("", Template.Cast<object>()) + "#{/each}";
        }

        public override IEnumerable<string> GetArguments()
            => Collection.GetArguments();
    }
}