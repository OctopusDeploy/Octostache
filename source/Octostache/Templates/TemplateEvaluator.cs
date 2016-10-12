using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Octostache.Templates
{
    class TemplateEvaluator
    {
        private readonly List<string> _missingTokens = new List<string>(); 

        public static void Evaluate(Template template, EvaluationContext context, out string[] missingTokens)
        {
            var evaluator = new TemplateEvaluator();
            evaluator.Evaluate(template.Tokens, context);
            missingTokens = evaluator._missingTokens.Distinct().ToArray();
        }

        public static void Evaluate(Template template, Binding properties, TextWriter output, out string[] missingTokens)
        {
            var context = new EvaluationContext(properties, output);
            Evaluate(template, context, out missingTokens);
        }

        void Evaluate(IEnumerable<TemplateToken> tokens, EvaluationContext context) 
        {
            foreach (var token in tokens)
            {
                Evaluate(token, context);
            }
        }

        void Evaluate(TemplateToken token, EvaluationContext context)
        {
            var tt = token as TextToken;
            if (tt != null)
            {
                EvaluateTextToken(context, tt);
                return;
            }

            var st = token as SubstitutionToken;
            if (st != null)
            {
                EvaluateSubstitutionToken(context, st);
                return;
            }

            var ct = token as ConditionalToken;
            if (ct != null)
            {
                EvaluateConditionalToken(context, ct);
                return;
            }

            var rt = token as RepetitionToken;
            if (rt != null)
            {
                EvaluateRepititionToken(context, rt);
                return;
            }

            throw new NotImplementedException("Unknown token type: " + token);
        }

        void EvaluateRepititionToken(EvaluationContext context, RepetitionToken rt)
        {
            string[] innerTokens;
            var items = context.ResolveAll(rt.Collection, out innerTokens).ToArray();
            _missingTokens.AddRange(innerTokens);

            for (var i = 0; i < items.Length; ++i)
            {
                var item = items[i];

                var specials = new Dictionary<string, string>();

                if (i == 0)
                    specials.Add(Constants.Each.First, "True");

                if (i == items.Length - 1)
                    specials.Add(Constants.Each.Last, "True");

                var locals = PropertyListBinder.CreateFrom(specials);

                locals.Add(rt.Enumerator.Text, item);

                var newContext = context.BeginChild(locals);
                Evaluate(rt.Template, newContext);
            }
        }

        void EvaluateConditionalToken(EvaluationContext context, ConditionalToken ct)
        {
            string[] innerTokens;
            var leftSide = context.Resolve(ct.Token.LeftSide, out innerTokens);
            _missingTokens.AddRange(innerTokens);

            var eqToken = ct.Token as ConditionalStringExpressionToken;
            if (eqToken != null)
            {
                var comparer = eqToken.Equality ? new Func<string, string, bool>((x, y) => x == y) : (x, y) => x != y;

                if(comparer(leftSide, eqToken.RightSide))
                    Evaluate(ct.TruthyTemplate, context);
                else
                    Evaluate(ct.FalsyTemplate, context);

                return;
            }

            var symToken = ct.Token as ConditionalSymbolExpressionToken;
            if (symToken != null)
            {
                var comparer = symToken.Equality ? new Func<string, string, bool>((x, y) => x == y) : (x, y) => x != y;

                string[] innerTokns;
                var rightSide = context.Resolve(symToken.RightSide, out innerTokns);
                _missingTokens.AddRange(innerTokens);

                if (comparer(leftSide, rightSide))
                    Evaluate(ct.TruthyTemplate, context);
                else
                    Evaluate(ct.FalsyTemplate, context);

                return;
            }

            if (IsTruthy(leftSide))
                Evaluate(ct.TruthyTemplate, context);
            else
                Evaluate(ct.FalsyTemplate, context);
        }

        void EvaluateSubstitutionToken(EvaluationContext context, SubstitutionToken st)
        {
            var value = Calculate(st.Expression, context);
            if (value == null)
            {
                _missingTokens.Add(st.ToString());
            }
            context.Output.Write(value ?? st.ToString());
        }

        static void EvaluateTextToken(EvaluationContext context, TextToken tt)
        {
            foreach (var text in tt.Text)
            {
                context.Output.Write(text);
            }
        }

        string Calculate(ContentExpression expression, EvaluationContext context)
        {
            var sx = expression as SymbolExpression;
            if (sx != null)
            {
                string[] innerTokens;
                var resolvedSymbol = context.ResolveOptional(sx, out innerTokens);
                _missingTokens.AddRange(innerTokens);
                return resolvedSymbol;
            }

            var fx = expression as FunctionCallExpression;
            if (fx == null)
            {
                throw new NotImplementedException("Unknown expression type: " + expression);
            }

            var argument = Calculate(fx.Argument, context);
            if (argument == null)
                return null; // If any argument is undefined, we fail the whole shebang

            var args = fx.Options.Select(opt => opt.Text).ToList();
            args.Insert(0, argument);
            return BuiltInFunctions.InvokeOrNull(fx.Function, args.ToArray());
        }

        
        static bool IsTruthy(string value)
        {


            return value != "0" &&
                value != "" &&
                string.Compare(value, "no", StringComparison.OrdinalIgnoreCase) != 0 &&
                string.Compare(value, "false", StringComparison.OrdinalIgnoreCase) != 0;
        }
    }
}
