using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;

#if NET40
#else
using System.Diagnostics.CodeAnalysis;
#endif

namespace Octostache.Templates
{
    public static class TemplateParser
    {
        static readonly Parser<Identifier> Identifier = Parse
            .Char(c => char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c) || c == '_' || c == '-' || c == ':' || c == '/' || c == '~' || c == '(' || c == ')', "identifier")
            .Except(Parse.WhiteSpace.FollowedBy("|"))
            .Except(Parse.WhiteSpace.FollowedBy("}"))
            .ExceptWhiteSpaceBeforeKeyword()
            .AtLeastOnce()
            .Text()
            .Select(s => new Identifier(s.Trim()))
            .WithPosition();

        static readonly Parser<Identifier> IdentifierWithoutWhitespace = Parse
            .Char(c => char.IsLetter(c) || char.IsDigit(c) || c == '_' || c == '-' || c == ':' || c == '/' || c == '~' || c == '(' || c == ')', "identifier")
            .Except(Parse.WhiteSpace.FollowedBy("|"))
            .Except(Parse.WhiteSpace.FollowedBy("}"))
            .ExceptWhiteSpaceBeforeKeyword()
            .AtLeastOnce()
            .Text()
            .Select(s => new Identifier(s.Trim()))
            .WithPosition();

        static readonly Parser<string> LDelim = Parse.String("#{").Except(Parse.String("#{/")).Text();
        static readonly Parser<string> RDelim = Parse.String("}").Text();

        static readonly Parser<SubstitutionToken> Substitution =
            (from leftDelim in LDelim
                from expression in Expression.Token()
                from rightDelim in RDelim
                select new SubstitutionToken(expression))
            .WithPosition();

        static readonly Parser<Indexer> SymbolIndexer =
            (from index in Substitution.Token()
                where index.Expression is SymbolExpression
                select new Indexer((SymbolExpression) index.Expression))
            .WithPosition();

        // Parsing the string of the Index, recursively parse any nested index in the string.
        // Eg: "Package[containers[0].container].Registry"
        static readonly Parser<Indexer> StringIndexer =
            from open in Parse.Char('[')
            from parts in (from indexer in StringIndexer
                    select indexer.ToString())
                .Or(Parse.CharExcept(new[] { ']', '[' }).AtLeastOnce().Text())
                .Many()
                .Token()
            from close in Parse.Char(']')
            select new Indexer(string.Join("", parts));

        static readonly Parser<Indexer> Indexer =
            (from index in
                    (from open in Parse.Char('[')
                        from index in SymbolIndexer.Token()
                        from close in Parse.Char(']')
                        select index) // NonEmpty Symbol Index
                    .Or(from index in StringIndexer
                        select index)
                    .WithPosition() // NonEmpty String Index
                    .Or(from open in Parse.Char('[')
                        from close in Parse.Char(']')
                        select new Indexer(string.Empty)) //Empty Index
                select index)
            .WithPosition()
            .Named("indexer");

        static readonly Parser<SymbolExpressionStep> TrailingStep =
            Parse.Char('.').Then(_ => Identifier).Select(i => (SymbolExpressionStep) i)
                .XOr(Indexer);

        static readonly Parser<SymbolExpression> Symbol =
            (from first in Identifier
                from rest in TrailingStep.Many()
                select new SymbolExpression(new[] { first }.Concat(rest)))
            .WithPosition();

        // Some trickery applied here to prevent a left-recursive definition
        static readonly Parser<FunctionCallExpression> FilterChain =
            from symbol in Symbol.Token().Optional().Select(s => s.IsDefined ? s.Get() : new SymbolExpression(new SymbolExpressionStep[] { }))
            from chain in Parse.Char('|').Then(_ =>
                from fn in IdentifierWithoutWhitespace.Named("filter").WithPosition().Token()
                from option in
                    Conditional.Select(t => (TemplateToken) t)
                        .Or(Repetition)
                        .Or(Substitution)
                        .Or(IdentifierWithoutWhitespace.Token().Select(t => t.Text)
                            .Or(QuotedText)
                            .Or(EscapedQuotedText)
                            .Select(t => new TextToken(t)))
                        .Named("option").Many().Optional()
                select new { Function = fn.Text, options = option }
            ).AtLeastOnce()
            select (FunctionCallExpression) chain.Aggregate((ContentExpression) symbol,
                (leftToken, fn) => new FunctionCallExpression(true, fn.Function, leftToken, fn.options.Get().ToArray()));

        static readonly Parser<ContentExpression> Expression =
            FilterChain.Select(c => (ContentExpression) c)
                .Or(Symbol);

        static readonly Parser<ConditionalToken> Conditional =
            (from leftDelim in LDelim
                from sp1 in Parse.WhiteSpace.Many()
                from kw in Keyword("if").Or(Keyword("unless"))
                from sp in Parse.WhiteSpace.AtLeastOnce()
                from expression in TokenMatch.Token().Or(StringMatch.Token()).Or(TruthyMatch.Token())
                from sp2 in Parse.WhiteSpace.Many()
                from rightDelim in RDelim
                from truthy in Parse.Ref(() => IfTemplate)
                from elseMatch in
                    (from el in Parse.String("#{else}")
                        from template in Parse.Ref(() => Template)
                        select template).Optional()
                from end in Parse.String("#{/" + kw + "}")
                let falsey = elseMatch.IsDefined ? elseMatch.Get() : Enumerable.Empty<TemplateToken>()
                select kw == "if" ? new ConditionalToken(expression, truthy, falsey) : new ConditionalToken(expression, falsey, truthy))
            .WithPosition();

        static readonly Parser<ConditionalExpressionToken> TruthyMatch =
            (from expression in Symbol.Token()
                select new ConditionalExpressionToken(expression))
            .WithPosition();

        static readonly Parser<ConditionalExpressionToken> StringMatch =
            (from expression in Symbol.Token()
                from eq in Keyword("==").Token().Or(Keyword("!=").Token())
                from compareTo in QuotedText.Token().Or(EscapedQuotedText.Token())
                let isEq = eq == "=="
                select new ConditionalStringExpressionToken(expression, isEq, compareTo))
            .WithPosition();

        static readonly Parser<ConditionalExpressionToken> TokenMatch =
            (from expression in Symbol.Token()
                from eq in Keyword("==").Token().Or(Keyword("!=").Token())
                from compareTo in Symbol.Token()
                let isEq = eq == "=="
                select new ConditionalSymbolExpressionToken(expression, isEq, compareTo))
            .WithPosition();

        static readonly Parser<RepetitionToken> Repetition =
            (from leftDelim in LDelim
                from sp1 in Parse.WhiteSpace.Many()
                from keyEach in Keyword("each")
                from sp2 in Parse.WhiteSpace.AtLeastOnce()
                from enumerator in Identifier.Token()
                from keyIn in Keyword("in").Token()
                from expression in Symbol.Token()
                from rightDelim in RDelim
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

        static readonly Parser<string> QuotedText =
            (from open in Parse.Char('"')
                from content in Parse.CharExcept(new[] { '"', '#' }).Many().Text()
                from close in Parse.Char('"')
                select content).Token();

        static readonly Parser<string> EscapedQuotedText =
            (from open in Parse.String("\\\"")
                from content in Parse.AnyChar.Until(Parse.String("\\\"")).Text()
                select content).Token();

        static readonly Parser<TemplateToken> Token =
            Conditional.Select(t => (TemplateToken) t)
                .Or(Repetition)
                .Or(Substitution)
                .Or(Text);

        static readonly Parser<TemplateToken[]> Template =
            Token.Many().Select(tokens => tokens.ToArray());

        static readonly Parser<TemplateToken[]> IfTemplate =
            Token.Except(Parse.String("#{else}")).Many().Select(tokens => tokens.ToArray());

        static readonly Parser<TemplateToken[]> ContinueOnErrorsTemplate =
            Token.ContinueMany().Select(tokens => tokens.ToArray());

        static readonly ItemCache<TemplateWithError> TemplateCache = new ItemCache<TemplateWithError>("OctostacheTemplate", 100, TimeSpan.FromMinutes(10));
        static readonly ItemCache<TemplateWithError> TemplateContinueCache = new ItemCache<TemplateWithError>("OctostacheTemplate", 100, TimeSpan.FromMinutes(10));
        static readonly ItemCache<SymbolExpression> PathCache = new ItemCache<SymbolExpression>("OctostachePath", 100, TimeSpan.FromMinutes(10));

        static Parser<T> FollowedBy<T>(this Parser<T> parser, string lookahead)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

            return i =>
            {
                var result = parser(i);
                if (!result.WasSuccessful)
                    return result;

                // ReSharper disable once ArrangeRedundantParentheses
                if (result.Remainder.Position >= (i.Source.Length - lookahead.Length))
                    return Result.Failure<T>(result.Remainder, "end of input reached while expecting lookahead", new[] { lookahead });

                var next = i.Source.Substring(result.Remainder.Position, lookahead.Length);
                return next != lookahead
                    ? Result.Failure<T>(result.Remainder, $"unexpected {next}", new[] { lookahead })
                    : result;
            };
        }

        static Parser<char> ExceptWhiteSpaceBeforeKeyword(this Parser<char> parser)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

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
                        return Result.Failure<char>(result.Remainder, $"unexpected keyword used {keyword}", new[] { keyword });
                    }
                }

                return result;
            };
        }

        static Parser<string> Keyword(string text)
        {
            return Parse.IgnoreCase(text).Text().Select(t => t.ToLowerInvariant());
        }

        static Parser<T> WithPosition<T>(this Parser<T> parser) where T : IInputToken
        {
            return i =>
            {
                var r = parser(i);
                if (r.WasSuccessful)
                    // ReSharper disable once PossibleStructMemberModificationOfNonVariableStruct
                    r.Value.InputPosition = new Position(i.Position, i.Line, i.Column);

                return r;
            };
        }

        // ReSharper disable once UnusedMember.Local
        // Used by the Test framework, to clear the caches before each test - as it is a static collection.
        static void ClearCache()
        {
            TemplateCache.Clear();
            TemplateContinueCache.Clear();
            PathCache.Clear();
        }

        /// <summary>
        /// Gets the names of variable replacement arguments that are resolvable by inspection of the template.
        /// This excludes variables referenced inside and iterator (foreach) as the items cannot be determined without the
        /// actual variable collection. The collection itself is returned.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="haltOnError"></param>
        /// <returns></returns>
        public static HashSet<string> ParseTemplateAndGetArgumentNames(string template, bool haltOnError = true)
        {
            var parser = haltOnError ? Template : ContinueOnErrorsTemplate;
            var templateTokens = parser.End().Parse(template);

            return new HashSet<string>(templateTokens.SelectMany(t => t.GetArguments()));
        }

        public static Template ParseTemplate(string template)
        {
            if (TryParseTemplate(template, out var result, out var error))
            {
                return result;
            }

            throw new ArgumentException($"Invalid template: {error}", nameof(template));
        }

        public static bool TryParseTemplate(string template, [NotNullWhen(true)] out Template? result, [NotNullWhen(false)] out string? error, bool haltOnError = true)
        {
            var parser = haltOnError ? Template : ContinueOnErrorsTemplate;
            var cache = haltOnError ? TemplateCache : TemplateContinueCache;

            var item = cache.GetOrAdd(template,
                () =>
                {
                    var tokens = parser.End().TryParse(template);
                    return new TemplateWithError
                    {
                        Result = tokens.WasSuccessful ? new Template(tokens.Value) : null,
                        Error = tokens.WasSuccessful ? null : tokens.ToString(),
                    };
                });

            error = item?.Error;
            result = item?.Result;
            return result != null && error == null;
        }

        internal static bool TryParseIdentifierPath(string path, [NotNullWhen(true)] out SymbolExpression? expression)
        {
            expression = PathCache.GetOrAdd(path,
                () =>
                {
                    var result = Symbol.TryParse(path);
                    return result.WasSuccessful ? result.Value : null;
                });

            return expression != null;
        }

        // A copy of Sprache's built in Many but when it hits an error the unparsed text is returned
        // as a text token and we continue
        // ReSharper disable once MemberCanBePrivate.Global
        public static Parser<IEnumerable<TemplateToken>> ContinueMany(this Parser<TemplateToken> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

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

        // ReSharper disable once MemberCanBePrivate.Global
        public static string Consumed(IInput before, IInput after) => before.Source.Substring(before.Position, after.Position - before.Position);

        class TemplateWithError
        {
            public Template? Result { get; set; }
            public string? Error { get; set; }
        }
    }
}
