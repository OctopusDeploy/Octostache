using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using Sprache;

namespace Octostache.Templates
{
    static class TemplateParser
    {
        static readonly Parser<Identifier> Identifier = Parse
            .Char(c => char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c) || c == '_' || c == '-' || c == ':' || c == '/' || c == '~' || c == '(' || c == ')', "identifier")
            .Except(Parse.WhiteSpace.FollowedBy("|"))
            .ExceptWhiteSpaceBeforeKeyword()
            .AtLeastOnce()
            .Text()
            .Select(s => new Identifier(s))
            .WithPosition();

        static readonly Parser<string> LDelim = Parse.String("#{").Except(Parse.String("#{/")).Text();
        static readonly Parser<string> RDelim = Parse.String("}").Text();

        static readonly Parser<SubstitutionToken> Substitution =
            (from ldelim in LDelim
             from expression in Expression.Token()
             from rdelim in RDelim
             select new SubstitutionToken(expression))
                .WithPosition();

        static readonly Parser<Indexer> StringIndexer =
            (from index in Parse.CharExcept(']').AtLeastOnce().Text()
             select new Indexer(index))
                .WithPosition();

        static readonly Parser<Indexer> SymbolIndexer =
            (from index in Substitution.Token()
             where index.Expression is SymbolExpression
             select new Indexer(index.Expression as SymbolExpression))
                .WithPosition();

        static readonly Parser<Indexer> Indexer =
            (from open in Parse.Char('[')
             from index in SymbolIndexer.Token().Or(StringIndexer.Token())
             from close in Parse.Char(']')
             select index)
                .WithPosition()
                .Named("indexer");

        static readonly Parser<SymbolExpressionStep> TrailingStep =
            Parse.Char('.').Then(_ => Identifier).Select(i => (SymbolExpressionStep)i)
                .XOr(Indexer);

        static readonly Parser<SymbolExpression> Symbol =
            (from first in Identifier
             from rest in TrailingStep.Many()
             select new SymbolExpression(new[] { first }.Concat(rest)))
                .WithPosition();

        // Some trickery applied here to prevent a left-recursive definition
        static readonly Parser<FunctionCallExpression> FilterChain =
            from symbol in Symbol.Token()
            from chain in Parse.Char('|').Then(_ => Identifier.Named("filter").WithPosition().Token()).AtLeastOnce()
            select (FunctionCallExpression)chain.Aggregate((ContentExpression)symbol, (c, id) => new FunctionCallExpression(true, id.Text, c));

        static readonly Parser<ContentExpression> Expression =
            FilterChain.Select(c => (ContentExpression)c)
            .Or(Symbol);

        static Parser<T> FollowedBy<T>(this Parser<T> parser, string lookahead)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");

            return i =>
            {
                var result = parser(i);
                if (!result.WasSuccessful)
                    return result;

                if (result.Remainder.Position >= (i.Source.Length - lookahead.Length))
                    return Result.Failure<T>(result.Remainder, "end of input reached while expecting lookahead", new[] { lookahead });

                var next = i.Source.Substring(result.Remainder.Position, lookahead.Length);
                if (next != lookahead)
                    return Result.Failure<T>(result.Remainder, string.Format("unexpected {0}", next), new[] { lookahead });

                return result;
            };
        }

        static Parser<char> ExceptWhiteSpaceBeforeKeyword(this Parser<char> parser)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");

            return i =>
            {
                var result = parser(i);
                if (!result.WasSuccessful || !char.IsWhiteSpace(result.Value))
                    return result;

                foreach (var keyword in new[] { "in", "==", "!=" })
                {
                    var length = keyword.Length;
                    if (i.Source.Length <= result.Remainder.Position + length)
                        continue;

                    if (!char.IsWhiteSpace(i.Source[result.Remainder.Position + length]))
                        continue;

                    var match = i.Source.Substring(result.Remainder.Position, length);
                    if (match == keyword)
                    {
                        return Result.Failure<char>(result.Remainder, string.Format("unexpected keyword used {0}", keyword),
                            new[] { keyword });
                    }
                }

                return result;
            };
        }

        static Parser<string> Keyword(string text)
        {
            return Parse.IgnoreCase(text).Text().Select(t => t.ToLowerInvariant());
        }

        static readonly Parser<ConditionalToken> Conditional =
            (from ldelim in LDelim
             from kw in Keyword("if").Or(Keyword("unless"))
             from sp in Parse.WhiteSpace.AtLeastOnce()
             from expression in TokenMatch.Token().Or(StringMatch.Token()).Or(TruthyMatch.Token())
             from rdelim in RDelim
             from truthy in Parse.Ref(() => Template)
             from end in Parse.String("#{/" + kw + "}")
             select kw == "if" ?
                 new ConditionalToken(expression, truthy, Enumerable.Empty<TemplateToken>()) :
                 new ConditionalToken(expression, Enumerable.Empty<TemplateToken>(), truthy))
                .WithPosition();

        static readonly Parser<ConditionalExpressionToken> TruthyMatch =
            (from expression in Symbol.Token()
             select new ConditionalExpressionToken(expression))
                .WithPosition();

        static readonly Parser<ConditionalExpressionToken> TokenMatch =
            (from expression in Symbol.Token()
             from _eq in Keyword("==").Token().Or(Keyword("!=").Token())
             from compareTo in QuotedText.Token()
             let eq = _eq == "=="
             select new ConditionalStringExpressionToken(expression, eq, compareTo))
                .WithPosition();

        static readonly Parser<ConditionalExpressionToken> StringMatch =
            (from expression in Symbol.Token()
             from _eq in Keyword("==").Token().Or(Keyword("!=").Token())
             from compareTo in Symbol.Token()
             let eq = _eq == "=="
             select new ConditionalSymbolExpressionToken(expression, eq, compareTo))
                .WithPosition();

        static readonly Parser<RepetitionToken> Repetition =
            (from ldelim in LDelim
             from _if in Keyword("each")
             from sp in Parse.WhiteSpace.AtLeastOnce()
             from enumerator in Identifier.Token()
             from _in in Keyword("in").Token()
             from expression in Symbol.Token()
             from rdelim in RDelim
             from body in Parse.Ref(() => Template)
             from end in Parse.String("#{/each}")
             select new RepetitionToken(expression, enumerator, body))
                .WithPosition();

        static readonly Parser<TextToken> Text =
            Parse.CharExcept('#').Select(c => c.ToString(CultureInfo.InvariantCulture))
                .Or(Parse.Char('#').End().Return("#"))
                .Or(Parse.String("##").FollowedBy("#{").Return("#"))
                .Or(Parse.String("##{").Select(c => "#{"))
                .Or(Parse.Char('#').Then(_ => Parse.CharExcept('{').Select(c => "#" + c)))
                .AtLeastOnce()
                .Select(s => new TextToken(s.ToList()))
                .WithPosition();

        public static readonly Parser<string> QuotedText =
            (from open in Parse.Char('"')
             from content in Parse.CharExcept(new[] { '"', '#' }).Many().Text()
             from close in Parse.Char('"')
             select content).Token();

        static readonly Parser<TemplateToken> Token =
            Conditional.Select(t => (TemplateToken)t)
                .Or(Repetition)
                .Or(Substitution)
                .Or(Text);

        static readonly Parser<TemplateToken[]> Template =
            Token.Many().Select(tokens => tokens.ToArray());

        static readonly Parser<TemplateToken[]> ContinueOnErrorsTemplate =
            Token.ContinueMany().Select(tokens => tokens.ToArray());

        static Parser<T> WithPosition<T>(this Parser<T> parser) where T : IInputToken
        {
            return i =>
            {
                var r = parser(i);
                if (r.WasSuccessful)
                    r.Value.InputPosition = new Position(i.Position, i.Line, i.Column);

                return r;
            };
        }

        static readonly MemoryCache Cache;

        static TemplateParser()
        {
            Cache = new MemoryCache("Octostache", new NameValueCollection() { { "CacheMemoryLimitMegabytes", (20 * 1024).ToString() } });
        }

        public static Template ParseTemplate(string template)
        {
            var cached = Cache.Get(template) as Template;
            if (cached == null)
            {
                cached = new Template(Template.End().Parse(template));
                Cache.Set(template, cached, new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(10) });
            }

            return cached;
        }

        public static bool TryParseTemplate(string template, out Template result, out string error, bool haltOnError = true)
        {
            var parser = haltOnError ? Template : ContinueOnErrorsTemplate;

            var cached = Cache.Get(template) as Template;
            if (cached == null)
            {
                var tokens = parser.End().TryParse(template);
                if (tokens.WasSuccessful)
                {
                    result = new Template(tokens.Value);
                    error = null;
                    cached = new Template(parser.End().Parse(template));
                    Cache.Set(template, cached, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) });
                    return true;
                }
                result = null;
                error = tokens.ToString();
                return false;
            }

            error = null;
            result = cached;
            return true;
        }

        internal static bool TryParseIdentifierPath(string path, out SymbolExpression expression)
        {
            var result = Symbol.TryParse(path);
            if (result.WasSuccessful)
            {
                expression = result.Value;
                return true;
            }
            expression = null;
            return false;
        }

        // A copy of Sprache's built in Many but when it hits an error the unparsed text is returned
        // as a text token and we continue
        public static Parser<IEnumerable<TemplateToken>> ContinueMany(this Parser<TemplateToken> parser)
        {
            if (parser == null) throw new ArgumentNullException("parser");

            return i =>
            {
                var remainder = i;
                var result = new List<TemplateToken>();
                var r = parser(i);

                while (true)
                {
                    if (remainder.Equals(r.Remainder))
                        break;

                    if (r.WasSuccessful)
                    {
                        result.Add(r.Value);
                    }
                    else
                    {
                        var consumed = Consumed(remainder, r.Remainder);
                        result.Add(new TextToken(new List<string> { consumed }));
                    }
                    remainder = r.Remainder;
                    r = parser(remainder);
                }

                return Result.Success<IEnumerable<TemplateToken>>(result, remainder);
            };
        }

        public static string Consumed(IInput before, IInput after)
        {
            return before.Source.Substring(before.Position, after.Position - before.Position);
        }
    }
}