using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
#if NET40
using System.Runtime.Caching;
#else
using Microsoft.Extensions.Caching.Memory;
#endif
using Sprache;

namespace Octostache.Templates
{
    static class TemplateParser
    {
        static readonly Parser<Identifier> Identifier = Parse
            .Char(c => char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c) || c == '_' || c == '-' || c == ':' || c == '/' || c == '~' || c == '(' || c == ')', "identifier")
            .Except(Parse.WhiteSpace.FollowedBy("|"))
            .Except(Parse.WhiteSpace.Many().FollowedBy("}"))
            .ExceptWhiteSpaceBeforeKeyword()
            .AtLeastOnce()
            .Text()
            .Select(s => new Identifier(s))
            .WithPosition();

        static readonly Parser<Identifier> IdentifierWithoutWhitespace = Parse
         .Char(c => char.IsLetter(c) || char.IsDigit(c) || c == '_' || c == '-' || c == ':' || c == '/' || c == '~' || c == '(' || c == ')', "identifier")
         .Except(Parse.WhiteSpace.FollowedBy("|"))
         .Except(Parse.WhiteSpace.Many().FollowedBy("}"))
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
        private static readonly Parser<FunctionCallExpression> FilterChain =
            from symbol in Symbol.Token().Optional().Select(s => s.IsDefined ? s.Get() : new SymbolExpression(new SymbolExpressionStep[0]))
            from chain in Parse.Char('|').Then(_ =>
                from fn in IdentifierWithoutWhitespace.Named("filter").WithPosition().Token()
                from option in
                    Conditional.Select(t => (TemplateToken)t)
                        .Or(Repetition)
                        .Or(Substitution)
                        .Or(IdentifierWithoutWhitespace.Token().Select(t => t.Text)
                            .Or(QuotedText)
                            .Select(t => new TextToken(t)))
                        .Named("option").Many().Optional()
                select new { Function = fn.Text, options = option }
                ).AtLeastOnce()
            select (FunctionCallExpression)chain.Aggregate((ContentExpression)symbol,
                (leftToken, fn) => new FunctionCallExpression(true, fn.Function, leftToken, fn.options.Get().ToArray()));


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
             from sp1 in Parse.WhiteSpace.Many()
             from kw in Keyword("if").Or(Keyword("unless"))
             from sp in Parse.WhiteSpace.AtLeastOnce()
             from expression in TokenMatch.Token().Or(StringMatch.Token()).Or(TruthyMatch.Token())
             from sp2 in Parse.WhiteSpace.Many()
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
             from compareTo in QuotedText.Token().Or(EscapedQuotedText.Token())
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
            Parse.CharExcept('#').Select(c => c.ToString())
                .Or(Parse.Char('#').End().Return("#"))
                .Or(Parse.String("##").FollowedBy("#{").Return("#"))
                .Or(Parse.String("##{").Select(c => "#{"))
                .Or(Parse.Char('#').Then(_ => Parse.CharExcept('{').Select(c => "#" + c)))
                .AtLeastOnce()
                .Select(s => new TextToken(s.ToArray()))
                .WithPosition();

        public static readonly Parser<string> QuotedText =
            (from open in Parse.Char('"')
             from content in Parse.CharExcept(new[] { '"', '#' }).Many().Text()
             from close in Parse.Char('"')
             select content).Token();

        public static readonly Parser<string> EscapedQuotedText =
        (from open in Parse.String("\\\"")
            from content in Parse.AnyChar.Until(Parse.String("\\\"")).Text()
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


#if NET40
        static TemplateParser()
        {
            Cache = new MemoryCache("Octostache", new NameValueCollection() { { "CacheMemoryLimitMegabytes", (20 * 1024).ToString() } });
        }

        private static void AddToCache(string template, Template cached)
        {
            Cache.Set(template, cached, new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(10) });
        }

        private static Template GetFromCache(string template)
        {
            return Cache.Get(template) as Template;
        }

#else
         static TemplateParser()
         {
             //todo: there is currently no support for CacheMemoryLimitMegabytes or similar
             //todo: there is currently no support for naming the cache
             Cache = new MemoryCache(new MemoryCacheOptions());
         }
 
         private static void AddToCache(string template, Template cached)
         {
             Cache.Set(template, cached, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });
         }
         private static Template GetFromCache(string template)
         {
             return Cache.Get(template) as Template;
         }
#endif



        public static Template ParseTemplate(string template)
        {
            var cached = GetFromCache(template);
            if (cached == null)
            {
                cached = new Template(Template.End().Parse(template));
                AddToCache(template, cached);
            }

            return cached;
        }


        public static bool TryParseTemplate(string template, out Template result, out string error, bool haltOnError = true)
        {
            var parser = haltOnError ? Template : ContinueOnErrorsTemplate;

            var cached = GetFromCache(template);
            if (cached == null)
            {
                var tokens = parser.End().TryParse(template);
                if (tokens.WasSuccessful)
                {
                    result = new Template(tokens.Value);
                    error = null;
                    cached = new Template(parser.End().Parse(template));
                    AddToCache(template, cached);
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
                        result.Add(new TextToken(consumed));
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