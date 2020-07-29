using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using YamlDotNet.Serialization;

namespace Octostache.Tests
{
    public class FiltersFixture : BaseFixture
    {
        [Theory]
        [InlineData("#{foo | ToUpper}")]
        [InlineData("#{Foo.Bar | HtmlEscape}")]
        [InlineData("#{Foo.Bar | ToUpper}")]
        [InlineData("#{Foo.Bar | Markdown}")]
        [InlineData("#{Foo.Bar | MarkdownToHtml}")]
        public void UnmatchedSubstitutionsAreEchoed(string template)
        {
            string error;
            var result = new VariableDictionary().Evaluate(template, out error);
            result.Should().Be(template);
            error.Should().Be($"The following tokens were unable to be evaluated: '{template}'");
        }

        [Fact]
        public void UnknownFiltersAreEchoed()
        {
            var result = Evaluate("#{Foo | ToBazooka}", new Dictionary<string, string> { { "Foo", "Abc" } });
            result.Should().Be("#{Foo | ToBazooka}");
        }

        [Fact]
        public void UnknownFiltersWithOptionsAreEchoed()
        {
            var result = Evaluate("#{Foo | ToBazooka 6}", new Dictionary<string, string> { { "Foo", "Abc" } });
            result.Should().Be("#{Foo | ToBazooka 6}");
        }

        [Fact]
        public void FiltersAreApplied()
        {
            var result = Evaluate("#{Foo | ToUpper}", new Dictionary<string, string> { { "Foo", "Abc" } });
            result.Should().Be("ABC");
        }

        [Theory]
        [InlineData("A&'bc", "A&amp;&apos;bc")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void HtmlIsEscaped(string input, string expectedOutput)
        {
            var result = Evaluate("#{Foo | HtmlEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData("A b:c+d/e", "A%20b%3Ac%2Bd%2Fe")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void UriDataStringsAreEncoded(string input, string expectedOutput)
        {
            var result = Evaluate("#{Foo | UriDataEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData("A b:c+d/e", "A%20b:c+d/e")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void UriStringsAreEncoded(string input, string expectedOutput)
        {
            var result = Evaluate("#{Foo | UriEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedOutput);
        }

        [Fact]
        public void XmlIsEscaped()
        {
            var result = Evaluate("#{Foo | XmlEscape}", new Dictionary<string, string> { { "Foo", "A&'bc" } });
            result.Should().Be("A&amp;&apos;bc");
        }

        [Theory]
        [InlineData("Test\"Test", "Test\\\"Test", "Quotes")]
        [InlineData("Test\rTest", "Test\\rTest", "Carriage return")]
        [InlineData("Test\nTest", "Test\\nTest", "Linefeed")]
        [InlineData("Test\tTest", "Test\\tTest", "Tab")]
        [InlineData("Test\\Test", "Test\\\\Test", "Backslash")]
        public void JsonIsEscaped(string input, string expectedResult, string testName)
        {
            var result = Evaluate("#{Foo | JsonEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedResult);
        }
        
        [Theory]
        [InlineData("single'quote", "single''quote")]
        [InlineData("\\'", "\\''")]
        [InlineData("a\n\tb\n\tc\n\td", "a\n\n\tb\n\n\tc\n\n\td")]
        [InlineData("a\r\nb", "a\r\n\r\nb")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void YamlSingleQuoteIsEscaped(string input, string expectedResult)
        {
            var result = Evaluate("#{Foo | YamlSingleQuoteEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("double\"quote", "double\\\"quote")]
        [InlineData("\\", "\\\\")]
        [InlineData("\"", "\\\"")]
        [InlineData("\t", "\\t")]
        [InlineData("\n", "\\n")]
        [InlineData("\r\n", "\\r\\n")]
        [InlineData("a\n\tb\n\tc\n\td", "a\\n\\tb\\n\\tc\\n\\td")]
        [InlineData("a\r\nb", "a\\r\\nb")]
        [InlineData("single'quote", "single'quote")]
        [InlineData("我叫章鱼", "\\u6211\\u53eb\\u7ae0\\u9c7c")]
        [InlineData(null, "")]
        public void YamlDoubleQuoteIsEscaped(string input, string expectedResult)
        {
            var result = Evaluate("#{Foo | YamlDoubleQuoteEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expectedResult);
        }

        private class TestDocument
        {
            public string Key { get; set; }
        }
        
        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("\"")]
        [InlineData("'")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("我叫章鱼")]
        [InlineData("This\nis a more\r\n \"complicated\"\texample \\❤")]
        public void YamlDoubleQuotedStringsCanRoundTrip(string input)
        {
            var yaml = Evaluate("Key: \"#{Input | YamlDoubleQuoteEscape}\"", new Dictionary<string, string> { { "Input", input } });

            var doc = new DeserializerBuilder()
                .Build()
                .Deserialize<TestDocument>(yaml);

            doc.Key.Should().Be(input);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("\"")]
        [InlineData("'")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("我叫章鱼")]
        [InlineData("this contains six spaces\nand one line break")]
        public void YamlSingleQuotedStringsCanRoundTrip(string input)
        {
            var yaml = Evaluate("Key: '#{Input | YamlSingleQuoteEscape}'", new Dictionary<string, string> { { "Input", input } });

            var doc = new DeserializerBuilder()
                .Build()
                .Deserialize<TestDocument>(yaml);

            doc.Key.Should().Be(input);
        }

        [Theory]
        [InlineData("\r\n", "\n")]
        [InlineData("a\nb", "a\nb")]
        [InlineData("a \nb", "a\nb")]
        [InlineData("a\n\nb", "a\n\nb")]
        [InlineData("a\r\nb", "a\nb")]
        [InlineData("a\r\n\nb", "a\n\nb")]
        [InlineData("a \n\nb", "a\n\nb")]
        [InlineData("a\n\n\n\n\nb", "a\n\n\n\n\nb")]
        [InlineData("this contains six spaces\nand one line break", "this contains six spaces\nand one line break")]
        public void YamlSingleQuotedStringsCanRoundTripWithSideEffects(string input, string expected)
        {
            var yaml = Evaluate("Key: '#{Input | YamlSingleQuoteEscape}'", new Dictionary<string, string> { { "Input", input } });

            var doc = new DeserializerBuilder()
                .Build()
                .Deserialize<TestDocument>(yaml);

            doc.Key.Should().Be(expected);
        }

        [Theory]
        [InlineData("#{Foo | Markdown}")]
        [InlineData("#{Foo | MarkdownToHtml}")]
        public void MarkdownIsProcessed(string input)
        {
            var result = Evaluate(input, new Dictionary<string, string> { { "Foo", "_yeah!_" } });
            result.Trim().Should().Be("<p><em>yeah!</em></p>");
        }

        [Theory]
        [InlineData("#{Foo | Markdown}", "http://octopus.com", "<p><a href=\"http://octopus.com\">http://octopus.com</a></p>")]
        [InlineData("#{Foo | MarkdownToHtml}", "http://octopus.com", "<p><a href=\"http://octopus.com\">http://octopus.com</a></p>")]
        [InlineData("#{Foo | Markdown}", "[Some link](http://octopus.com)", "<p><a href=\"http://octopus.com\">Some link</a></p>")]
        [InlineData("#{Foo | MarkdownToHtml}", "[Some link](http://octopus.com)", "<p><a href=\"http://octopus.com\">Some link</a></p>")]
        public void MarkdownHttpLinkIsProcessed(string input, string value, string expectedResult)
        {
            var result = Evaluate(input, new Dictionary<string, string> { { "Foo", value } });
            result.Trim().Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("#{Foo | Markdown}")]
        [InlineData("#{Foo | MarkdownToHtml}")]
        public void MarkdownTablesAreProcessed(string input)
        {
            var dictionary = new Dictionary<string, string> { {"Foo",
@"|Header1|Header2|
|-|-|
|Cell1|Cell2|" }};
            var result = Evaluate(input, dictionary);
            result.Trim().Should().Be("<table>\n<thead>\n<tr>\n<th>Header1</th>\n<th>Header2</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td>Cell1</td>\n<td>Cell2</td>\n</tr>\n</tbody>\n</table>");
        }

        [Fact]
        public void DateIsFormatted()
        {
            var dict = new Dictionary<string, string> { { "Foo", "2030/05/22 09:05:00" } };

            var result = Evaluate("#{Foo | Format Date \"HH dd-MMM-yyy\"}", dict);
            result.Should().Be("09 22-May-2030");
        }

        [Fact]
        public void DateFormattingCanUseInnerVariable()
        {
            var dict = new Dictionary<string, string> { { "Foo", "2030/05/22 09:05:00" }, { "Format", "HH dd-MMM-yyyy" } };

            var result = Evaluate("#{Foo | Format Date #{Format}}", dict);
            result.Should().Be("09 22-May-2030");
        }

        [Fact]
        public void GenericConverterAcceptsDouble()
        {
            var dict = new Dictionary<string, string> { { "Cash", "23.4" }};

            var result = Evaluate("#{Cash | Format Double C}", dict);
            result.Should().Be(23.4.ToString("C"));
        }

        [Fact]
        public void GenericConverterAcceptsDate()
        {
            var dict = new Dictionary<string, string> { { "MyDate", "2030/05/22 09:05:00" } };
            var result = Evaluate("#{MyDate | Format DateTime \"HH dd-MMM-yyyy\" }", dict);
            result.Should().Be("09 22-May-2030");
        }

        [Fact]
        public void EscaoedFilterStringAccepted()
        {
            var dict = new Dictionary<string, string> { { "MyDate", "2030/05/22 09:05:00" }};
            var result = Evaluate("#{MyDate | Format DateTime \\\"HH dd-MMM-yyyy\\\" }", dict);
            result.Should().Be("09 22-May-2030");
        }

        [Fact]
        public void FormatFunctionDefaultAssumeDecimal()
        {
            var dict = new Dictionary<string, string> { { "Cash", "23.4" } };

            var result = Evaluate("#{Cash | Format C}", dict);
            result.Should().Be(23.4.ToString("C"));
        }

        [Fact]
        public void FormatFunctionWillTryDefaultDateTimeIfNotDecimal()
        {
            var dict = new Dictionary<string, string> { { "Date", "2030/05/22 09:05:00" } };
            var result = Evaluate("#{ Date | Format yyyy}", dict);
            result.Should().Be("2030");
        }

        [Fact]
        public void FormatFunctionWillReturnUnreplacedIfNoDefault()
        {
            var dict = new Dictionary<string, string> { { "Invalid", "hello World" } };

            var result = Evaluate("#{Invalid | Format yyyy}", dict);
            result.Should().Be("#{Invalid | Format yyyy}");
        }

        [Fact]
        public void NowDateReturnsNow()
        {
            var result = Evaluate("#{ | NowDate}", new Dictionary<string, string> ());
            DateTime.Parse(result).Should().BeCloseTo(DateTime.Now, 60000);
        }

        [Fact]
        public void NowDateCanBeFormatted()
        {
            var result = Evaluate("#{ | NowDate yyyy}", new Dictionary<string, string>());
            result.Should().Be(DateTime.Now.Year.ToString());
        }

        [Fact]
        public void NullJsonPropertyTreatedAsEmptyString()
        {
            var result = Evaluate("Alpha#{Foo.Bar | ToUpper}bet", new Dictionary<string, string> { { "Foo", "{Bar: null}" } });
            result.Should().Be("Alphabet");
        }

        [Fact]
        public void NowDateCanBeChained()
        {
            var result = Evaluate("#{ | NowDate | Format Date MMM}", new Dictionary<string, string>());
            result.Should().Be(DateTime.Now.ToString("MMM"));
        }

        [Fact]
        public void NowDateReturnsNowInUtc()
        {
            var result = Evaluate("#{ | NowDateUtc}", new Dictionary<string, string>());
            DateTimeOffset.Parse(result).Should().BeCloseTo(DateTimeOffset.UtcNow, 60000);
        }

        [Fact]
        public void NowDateUtcCanBeChained()
        {
            var result = Evaluate("#{ | NowDateUtc | Format DateTimeOffset zz}", new Dictionary<string, string>());
            result.Should().Be("+00");

            var result1 = Evaluate("#{ | NowDate | Format DateTimeOffset zz}", new Dictionary<string, string>());
            result1.Should().Be(DateTimeOffset.Now.ToString("zz"));
        }

        [Fact]
        public void FiltersAreAppliedInOrder()
        {
            var result = Evaluate("#{Foo|ToUpper|ToLower}", new Dictionary<string, string> { { "Foo", "Abc" } });
            result.Should().Be("abc");
        }

        [Fact]
        public void StringsCanBeBase64Encoded()
        {
            var dict = new Dictionary<string, string> { { "String", "Foo Bar" } };
            var result = Evaluate("#{String | tobase64}",dict);
            result.Should().Be("Rm9vIEJhcg==");
        }

        [Fact]
        public void Replace()
        {
            var result = Evaluate("#{foo | Replace abc def}", new Dictionary<string, string> { { "foo", "abc" } });
            result.Should().Be("def");
        }

        [Fact]
        public void ReplaceWithEmptyString()
        {
            var result = Evaluate("#{foo | Replace a}", new Dictionary<string, string> { { "foo", "abc" } });
            result.Should().Be("bc");
        }

        [Fact]
        public void ReplaceDoesNothing()
        {
            var result = Evaluate("#{foo | Replace}", new Dictionary<string, string> { { "foo", "abc" } });
            result.Should().Be("#{foo | Replace}");
        }

        [Fact]
        public void ReplaceIsCaseSensitive()
        {
            var result = Evaluate("#{foo | Replace abc def}", new Dictionary<string, string> { { "foo", "ABC" } });
            result.Should().Be("ABC");
        }

        [Fact]
        public void ReplaceHandlesSpaces()
        {
            var result = Evaluate(@"#{foo | Replace ""ab c"" ""d ef""}", new Dictionary<string, string> { { "foo", "ab c" } });
            result.Should().Be("d ef");
        }

        [Fact]
        public void ReplaceWorksWithVariableOptions()
        {
            var result = Evaluate("#{foo | Replace #{regex}#{replacement}}", new Dictionary<string, string>
            {
                { "foo", "abc" },
                { "regex", "b"},
                { "replacement", "x"}
            });
            result.Should().Be(@"axc");
        }

        [Fact]
        public void ReplaceHandlesDoubleQuotesViaNestedSubsitution()
        {
            var result = Evaluate("#{foo | Replace #{regex}#{replacement}}", new Dictionary<string, string>
            {
                { "foo", @"a""b" },
                { "regex", @"a"""},
                { "replacement", @"""c"}
            });
            result.Should().Be(@"""cb");
        }


        [Fact]
        public void ReplaceHandlesSingleQuotes()
        {
            var result = Evaluate(@"#{foo | Replace ""a'b"" ""d'e""}", new Dictionary<string, string> { { "foo", "a'b" } });
            result.Should().Be("d'e");
        }

        [Fact]
        public void ReplaceRange()
        {
            var result = Evaluate(@"#{foo | Replace ""[a-z]+"" 1}", new Dictionary<string, string> { { "foo", "a'b" } });
            result.Should().Be("1'1");
        }

        [Fact]
        public void ReplaceHandlesEscapingRegexSpecialCharacter()
        {
            var result = Evaluate(@"#{foo | Replace ""a\(b"" ""d(e""}", new Dictionary<string, string> { { "foo", @"a(b" } });
            result.Should().Be(@"d(e");
        }

        [Fact]
        public void ReplaceCanSubstitute()
        {
            var result = Evaluate(@"#{foo | Replace ""o(.+)o([a-z]*)s"" ""o$2o$1s""}", new Dictionary<string, string> { { "foo", "opuocts" } });
            result.Should().Be("octopus");
        }

        [Fact]
        public void ReplaceCanDoMultipleSubstitutions()
        {
            var result = Evaluate(@"#{foo | Replace ""a"" x}", new Dictionary<string, string> { { "foo", "ababa" } });
            result.Should().Be("xbxbx");
        }


        [Fact]
        public void ReplaceAtStartOfLine()
        {
            var result = Evaluate(@"#{foo | Replace ""^a"" x}", new Dictionary<string, string> { { "foo", "ababa" } });
            result.Should().Be("xbaba");
        }

        [Fact]
        public void ReplaceAtEndOfLine()
        {
            var result = Evaluate(@"#{foo | Replace ""a$"" x}", new Dictionary<string, string> { { "foo", "ababa" } });
            result.Should().Be("ababx");
        }

        [Fact]
        public void SubstringDoesNothing()
        {
            var result = Evaluate(@"#{foo | Substring}", new Dictionary<string, string> { { "foo" , "ababa" } });
            result.Should().Be("#{foo | Substring}");
        }

        [Fact]
        public void SubstringWithTooManyArgumentsDoesNothing()
        {
            var result = Evaluate(@"#{foo | Substring 1 2 3}", new Dictionary<string, string> {{"foo", "ababa"}});
            result.Should().Be("#{foo | Substring 1 2 3}");
        }

        [Fact]
        public void SubstringWithOnlyLength()
        {
            var result = Evaluate(@"#{foo | Substring 7}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("Octopus");
        }

        [Fact]
        public void SubstringWithStartAndLength()
        {
            var result = Evaluate(@"#{foo | Substring 8 6}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("Deploy");
        }

        [Fact]
        public void SubstringHandlesNonNumericLength()
        {
            var result = Evaluate(@"#{foo | Substring a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring a}");
        }

        [Fact]
        public void SubstringHandlesNonNumericLengthWithStart()
        {
            var result = Evaluate(@"#{foo | Substring 0 a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring 0 a}");
        }

        [Fact]
        public void SubstringHandlesLengthIndexOutOfRange()
        {
            var result = Evaluate(@"#{foo | Substring 20}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring 20}");
        }

        [Fact]
        public void SubstringHandlesStartAndLengthIndexOutOfRange()
        {
            var result = Evaluate(@"#{foo | Substring 8 7}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring 8 7}");
        }

        [Fact]
        public void SubstringHandlesNegativeValueForLength()
        {
            var result = Evaluate(@"#{foo | Substring -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring -1}");
        }

        [Fact]
        public void SubstringHandlesNegativeStartAndLength()
        {
            var result = Evaluate(@"#{foo | Substring 0 -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Substring 0 -1}");
        }

        [Fact]
        public void TruncateDoesNothing()
        {
            var result = Evaluate(@"#{foo | Truncate}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Truncate}");
        }

        [Fact]
        public void TruncateDoesNothingWithLengthGreaterThanArgumentLength()
        {
            var result = Evaluate(@"#{foo | Truncate 50}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("Octopus Deploy");
        }

        [Fact]
        public void TruncateHandlesNonNumericLength()
        {
            var result = Evaluate(@"#{foo | Truncate a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Truncate a}");
        }

        [Fact]
        public void TruncateHandlesNegativeLength()
        {
            var result = Evaluate(@"#{foo | Truncate -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Truncate -1}");
        }

        [Fact]
        public void TruncateTruncatesArgumentToSpecifiedLength()
        {
            var result = Evaluate(@"#{foo | Truncate 7}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("Octopus...");
        }

        [Fact]
        public void TrimIsApplied()
        {
            var result = Evaluate(@"#{foo | Trim}", new Dictionary<string, string> { { "foo", " Octopus Deploy " } });
            result.Should().Be("Octopus Deploy");
        }

        [Fact]
        public void TrimStartIsApplied()
        {
            var result = Evaluate(@"#{foo | Trim start}", new Dictionary<string, string> { { "foo", " Octopus Deploy " } });
            result.Should().Be("Octopus Deploy ");
        }

        [Fact]
        public void TrimEndIsApplied()
        {
            var result = Evaluate(@"#{foo | Trim End}", new Dictionary<string, string> { { "foo", " Octopus Deploy " } });
            result.Should().Be(" Octopus Deploy");
        }

        [Fact]
        public void TrimCanBeChained()
        {
            var result = Evaluate(@"#{foo | Substring 8 | Trim}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("Octopus");
        }

        [Fact]
        public void TrimWithInvalidOptionDoesNoting()
        {
            var result = Evaluate(@"#{foo | Trim Both}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("#{foo | Trim Both}");
        }

        [Theory]
        [InlineData("/docs", "UriPart Host", "[UriPart Host error: This operation is not supported for a relative URI.]")]
        [InlineData("https://octopus.com/docs", "UriPart", "[UriPart error: no argument given]")]
        [InlineData("https://octopus.com/docs", "UriPart bar", "[UriPart bar error: argument 'bar' not supported]")]
        [InlineData("https://octopus.com/docs", "UriPart AbsolutePath", "/docs")]
        [InlineData("https://octopus.com/docs", "UriPart AbsoluteUri", "https://octopus.com/docs")]
        [InlineData("https://octopus.com/docs", "UriPart Authority", "octopus.com")]
        [InlineData("https://octopus.com/docs", "UriPart DnsSafeHost", "octopus.com")]
        [InlineData("https://octopus.com/docs#filters", "UriPart Fragment", "#filters")]
        [InlineData("https://octopus.com/docs", "UriPart Host", "octopus.com")]
        [InlineData("https://octopus.com/docs", "UriPart HostAndPort", "octopus.com:443")]
        [InlineData("https://octopus.com/docs", "UriPart HostNameType", "Dns")]
        [InlineData("https://octopus.com/docs", "UriPart IsAbsoluteUri", "true")]
        [InlineData("https://octopus.com/docs", "UriPart IsDefaultPort", "true")]
        [InlineData("https://octopus.com/docs", "UriPart IsFile", "false")]
        [InlineData("https://octopus.com/docs", "UriPart IsLoopback", "false")]
        [InlineData("https://octopus.com/docs", "UriPart IsUnc", "false")]
        [InlineData("https://octopus.com/docs", "UriPart Path", "/docs")]
        [InlineData("https://octopus.com/docs?filter=faq", "UriPart PathAndQuery", "/docs?filter=faq")]
        [InlineData("https://octopus.com/docs", "UriPart Port", "443")]
        [InlineData("https://octopus.com/docs?filter=faq", "UriPart Query", "?filter=faq")]
        [InlineData("https://octopus.com/docs", "UriPart Scheme", "https")]
        [InlineData("https://octopus.com/docs", "UriPart SchemeAndServer", "https://octopus.com")]
        [InlineData("https://username:password@octopus.com", "UriPart UserInfo", "username:password")]
        public void UriPart(string inputUrl, string inputFilterExpression, string expectedOutput)
        {
            var result = Evaluate($"#{{foo | {inputFilterExpression}}}", new Dictionary<string, string> { { "foo", inputUrl } });
            result.Should().Be(expectedOutput);
        }
    }
}
