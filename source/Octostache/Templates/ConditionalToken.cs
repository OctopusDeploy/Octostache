using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    /// <summary>
    /// Example: <code>#{if Octopus.IsCool}...#{/if}</code>
    /// Example: <code>#{if Octopus.CoolStatus != "Uncool"}...#{/if}</code>
    /// Example: <code>#{if Octopus.IsCool == Octostache.IsCool}...#{/if}</code>
    /// </summary>
    class ConditionalToken : TemplateToken
    {
        public ConditionalToken(ConditionalExpressionToken token, IEnumerable<TemplateToken> truthyBranch, IEnumerable<TemplateToken> falsyBranch)
        {
            Token = token;
            TruthyTemplate = truthyBranch.ToArray();
            FalsyTemplate = falsyBranch.ToArray();
        }

        public ConditionalExpressionToken Token { get; }

        public TemplateToken[] TruthyTemplate { get; }

        public TemplateToken[] FalsyTemplate { get; }

        public override string ToString()
        {
            return "#{if " + Token.LeftSide + Token.EqualityText + "}" + string.Join("", TruthyTemplate.Cast<object>()) + "#{else}" + string.Join("", FalsyTemplate.Cast<object>()) + "#{/if}";
        }

        public override IEnumerable<string> GetArguments() 
            => Token.GetArguments()
                .Concat(TruthyTemplate.SelectMany(t => t.GetArguments()))
                .Concat(FalsyTemplate.SelectMany(t => t.GetArguments()));
    }

    class ConditionalExpressionToken : TemplateToken
    {
        public SymbolExpression LeftSide { get; }
        public virtual string EqualityText => "";

        public ConditionalExpressionToken(SymbolExpression leftSide)
        {
            LeftSide = leftSide;
        }

        public override IEnumerable<string> GetArguments() 
              => LeftSide.GetArguments();
    }

    class ConditionalStringExpressionToken : ConditionalExpressionToken
    {
        public string RightSide { get; }
        public bool Equality { get; }
        public override string EqualityText => " " + (Equality ? "==" : "!=") + " " + RightSide + " ";

        public ConditionalStringExpressionToken(SymbolExpression leftSide, bool eq, string rightSide) : base(leftSide)
        {
            Equality = eq;
            RightSide = rightSide;
        }

        public override IEnumerable<string> GetArguments()
            => base.GetArguments().Concat(new[] {RightSide});
    }

    class ConditionalSymbolExpressionToken : ConditionalExpressionToken
    {
        public SymbolExpression RightSide { get; }
        public bool Equality { get; }
        public override string EqualityText => " " + (Equality ? "==" : "!=") + " " + RightSide + " ";

        public ConditionalSymbolExpressionToken(SymbolExpression leftSide, bool eq, SymbolExpression rightSide) : base(leftSide)
        {
            Equality = eq;
            RightSide = rightSide;
        }
        
        public override IEnumerable<string> GetArguments()
            => base.GetArguments().Concat(RightSide.GetArguments());
    }
}