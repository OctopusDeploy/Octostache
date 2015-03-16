using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    /// <summary>
    /// Example: <code>#{if Octopus.IsCool}...#{/if}</code>
    /// </summary>
    class ConditionalToken : TemplateToken
    {
        readonly SymbolExpression expression;
        readonly TemplateToken[] truthyTemplate;
        readonly TemplateToken[] falsyTemplate;

        public ConditionalToken(SymbolExpression expression, IEnumerable<TemplateToken> truthyBranch, IEnumerable<TemplateToken> falsyBranch)
        {
            this.expression = expression;
            truthyTemplate = truthyBranch.ToArray();
            falsyTemplate = falsyBranch.ToArray();
        }

        public SymbolExpression Expression
        {
            get { return expression; }
        }

        public TemplateToken[] TruthyTemplate
        {
            get { return truthyTemplate; }
        }

        public TemplateToken[] FalsyTemplate
        {
            get { return falsyTemplate; }
        }

        public override string ToString()
        {
            return "#{if " + Expression + "}" + string.Join("", TruthyTemplate.Cast<object>()) + "#{else}" + string.Join("", FalsyTemplate.Cast<object>()) + "#{/if}";
        }
    }
}