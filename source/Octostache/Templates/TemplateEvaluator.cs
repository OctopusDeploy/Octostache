using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Octostache.Templates
{
    class TemplateEvaluator
    {
        readonly List<string> missingTokens = new List<string>();
        readonly List<string> nullTokens = new List<string>();

        public static void Evaluate(Template template, EvaluationContext context, out string[] missingTokens, out string[] nullTokens, out ReplacementOcurrance[] replacements)
        {
            var evaluator = new TemplateEvaluator();
            evaluator.Evaluate(template.Tokens, context);
            missingTokens = evaluator.missingTokens.Distinct().ToArray();
            nullTokens = evaluator.nullTokens.Distinct().ToArray();
            replacements = evaluator.ReplacementOccurances.ToArray();
        }

        public static void Evaluate(Template template,
            Binding properties,
            TextWriter output,
            out string[] missingTokens,
            out string[] nullTokens, out ReplacementOcurrance[] replacements)
        {
            var context = new EvaluationContext(properties, output);
            Evaluate(template, context, out missingTokens, out nullTokens, out replacements);
        }

        public static void Evaluate(Template template,
            Binding properties,
            TextWriter output,
            Dictionary<string, Func<string?, string[], string?>> extensions,
            out string[] missingTokens,
            out string[] nullTokens, out ReplacementOcurrance[] replacements)
        {
            var context = new EvaluationContext(properties, output, extensions: extensions);
            Evaluate(template, context, out missingTokens, out nullTokens, out replacements);
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

            var cat = token as CalculationToken;
            if (cat != null)
            {
                var value = cat.Expression.Evaluate(s =>
                {
                    var value = context.ResolveOptional(s, out var innerTokens, out var _);
                    missingTokens.AddRange(innerTokens);
                    return value;
                });

                if (value != null)
                    context.Output.Write(value);
                else
                    context.Output.Write(cat.ToString());

                return;
            }

            throw new NotImplementedException("Unknown token type: " + token);
        }

        void EvaluateRepititionToken(EvaluationContext context, RepetitionToken rt)
        {
            string[] innerTokens;
            var items = context.ResolveAll(rt.Collection, out innerTokens, out _).ToArray();
            missingTokens.AddRange(innerTokens);

            for (var i = 0; i < items.Length; ++i)
            {
                var item = items[i];

                var specials = new Dictionary<string, string?>
                {
                    { Constants.Each.Index, i.ToString() },
                    { Constants.Each.First, i == 0 ? "True" : "False" },
                    { Constants.Each.Last, i == items.Length - 1 ? "True" : "False" },
                };

                var locals = PropertyListBinder.CreateFrom(specials);

                locals.Add(rt.Enumerator.Text, item);

                var newContext = context.BeginChild(locals);
                Evaluate(rt.Template, newContext);
            }
        }

        void EvaluateConditionalToken(EvaluationContext context, ConditionalToken ct)
        {
            string? leftSide;
            var leftToken = ct.Token.LeftSide as SymbolExpression;
            if (leftToken != null)
            {
                leftSide = context.Resolve(leftToken, out var innerTokens, out var _);
                missingTokens.AddRange(innerTokens);
            }
            else
            {
                leftSide = Calculate(ct.Token.LeftSide, context);
                if (leftSide == null)
                {
                    context.Output.Write(ct.ToString());
                    missingTokens.Add(ct.Token.LeftSide.ToString());
                    return;
                }
            }

            var eqToken = ct.Token as ConditionalStringExpressionToken;
            if (eqToken != null)
            {
                var comparer = eqToken.Equality ? new Func<string, string, bool>((x, y) => x == y) : (x, y) => x != y;

                if (comparer(leftSide, eqToken.RightSide))
                    Evaluate(ct.TruthyTemplate, context);
                else
                    Evaluate(ct.FalsyTemplate, context);

                return;
            }

            var symToken = ct.Token as ConditionalSymbolExpressionToken;
            if (symToken != null)
            {
                var comparer = symToken.Equality ? new Func<string, string, bool>((x, y) => x == y) : (x, y) => x != y;
                string? rightSide;

                var rightToken = symToken.RightSide as SymbolExpression;
                if (rightToken != null)
                {
                    rightSide = context.Resolve(rightToken, out var innerTokens, out var _);
                    missingTokens.AddRange(innerTokens);
                }
                else
                {
                    rightSide = Calculate(symToken.RightSide, context);
                    if (rightSide == null)
                    {
                        context.Output.Write(ct.ToString());
                        missingTokens.Add(symToken.RightSide.ToString());
                        return;
                    }
                }

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

        void EvaluateSubstitutionToken(EvaluationContext context, SubstitutionToken substitutionToken)
        {
            var value = Calculate(substitutionToken.Expression, context);
            
            var startPosn = (substitutionToken.InputPosition?.Pos ?? 0) + offset;
            var raw = substitutionToken.ToString();
            var endPosn = startPosn;
            offset -= raw.Length;
            if (value != null)
            {
                endPosn =+ value.Length;
                offset += value.Length;
            }
            ReplacementOccurances.Add(new ReplacementOcurrance()
            {
                Raw = raw,
                StartPosn = startPosn,
                EndPosn = endPosn,
            });
            
            
            
            
            if (value == null)
            {
                if (substitutionToken.Expression is FunctionCallExpression { Function: "null" } fx)
                {
                    nullTokens.Add(substitutionToken.ToString());
                }
                else
                {
                    missingTokens.Add(substitutionToken.ToString());
                }
            }

            context.Output.Write(value ?? substitutionToken.ToString());
        }

        static void EvaluateTextToken(EvaluationContext context, TextToken tt)
        {
            foreach (var text in tt.Text)
            {
                context.Output.Write(text);
            }
        }

        List<ReplacementOcurrance> ReplacementOccurances = new List<ReplacementOcurrance>();
        int offset = 0;
        string? Calculate(ContentExpression expression, EvaluationContext context)
        {
            if (expression is SymbolExpression sx)
            {
                var resolvedSymbol = context.ResolveOptional(sx, out var innerTokens, out var _);
                missingTokens.AddRange(innerTokens);

                /*var startPosn = (expression?.InputPosition?.Pos ?? 0) + offset -1;
                var endPosn = startPosn + resolvedSymbol.Length -1;
                var raw = expression.ToString();
                offset = offset - (raw.Length - resolvedSymbol.Length);
                s.Add(new VariableReplacement()
                {
                    Raw = raw,
                    StartPosn = startPosn,
                    EndPosn = endPosn,
                });*/
                return resolvedSymbol;
            }

            var fx = expression as FunctionCallExpression;
            if (fx == null)
            {
                throw new NotImplementedException("Unknown expression type: " + expression);
            }

            var argument = Calculate(fx.Argument, context);

            var args = fx.Options.Select(opt => Resolve(opt, context)).ToArray();

            var funcOut = BuiltInFunctions.InvokeOrNull(fx.Function, argument, args);
            if (funcOut != null)
            {
                return funcOut;
            }

            return InvokeOrNullExtension(context.Extensions, fx.Function, argument, args);
        }

        string? InvokeOrNullExtension(Dictionary<string, Func<string?, string[], string?>> extensions, string function, string? argument, string[] args)
        {
            var functionName = function.ToLowerInvariant();

            if (extensions.TryGetValue(functionName, out var ext))
                return ext(argument, args);

            return null;
        }

        string Resolve(TemplateToken token, EvaluationContext context)
        {
            using (var x = new StringWriter())
            {
                var c2 = new EvaluationContext(new Binding(), x, context);
                Evaluate(token, c2);
                x.Flush();
                return x.ToString();
            }
        }

        internal static bool IsTruthy(string value) => value != "0" && value != "" && !StringEqual(value.Trim(), "no") && !StringEqual(value.Trim(), "false");
        static bool StringEqual(string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
