using System;
using System.Collections.Generic;
using System.Linq;

namespace Octostache.Templates
{
    static class TemplateAnalyzer
    {
        static IEnumerable<string> GetDependencies(IEnumerable<TemplateToken> tokens, AnalysisContext context)
        {
            foreach (var token in tokens)
            {
                foreach (var dependency in GetDependencies(token, context))
                {
                    yield return dependency;
                }
            }
        }

        static IEnumerable<string> GetDependencies(TemplateToken token, AnalysisContext context)
        {
            if (token is TextToken)
                yield break;

            var st = token as SubstitutionToken;
            if (st != null)
            {
                foreach (var symbol in GetSymbols(st.Expression))
                    yield return context.Expand(symbol);
                yield break;
            }

            var ct = token as ConditionalToken;
            if (ct != null)
            {
                yield return context.Expand(ct.Token.LeftSide);
                var exp = ct.Token as ConditionalSymbolExpressionToken;
                if (exp != null)
                {
                    yield return context.Expand(exp.RightSide);
                }
                foreach (var templateDependency in GetDependencies(ct.TruthyTemplate.Concat(ct.FalsyTemplate), context))
                {
                    yield return templateDependency;
                }
                yield break;
            }

            var rt = token as RepetitionToken;
            if (rt != null)
            {
                var ctx = context.BeginChild(rt.Enumerator, rt.Collection);
                foreach (var dependency in GetDependencies(rt.Template, ctx))
                    yield return dependency;

                yield break;
            }

            throw new NotImplementedException("Unknown token type: " + token);
        }

        static IEnumerable<SymbolExpression> GetSymbols(ContentExpression expression)
        {
            var sx = expression as SymbolExpression;
            if (sx != null)
            {
                yield return sx;
            }
            else
            {
                var fx = expression as FunctionCallExpression;
                if (fx != null)
                {
                    foreach (var symbol in GetSymbols(fx.Argument))
                    {
                        yield return symbol;
                    }
                }
                else
                {
                    throw new NotImplementedException("Unknown expression type: " + expression);
                }
            }
        }
    }
}
