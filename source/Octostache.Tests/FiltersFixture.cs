using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

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

        [Fact]
        public void UriPartNoArgument()
        {
            var uri = @"https://octopus.com/docs";
            var result = Evaluate(@"#{foo | UriPart}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("[UriPart: no argument given]");
        }

        [Fact]
        public void UriPartInvalidArgument()
        {
            var uri = @"https://octopus.com/docs";
            var result = Evaluate(@"#{foo | UriPart bar}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("[UriPart: argument 'bar' not supported]");
        }

        [Fact]
        public void UriPartAbsolutePath()
        {
            var uri = @"https://octopus.com/docs";
            var result = Evaluate(@"#{foo | UriPart AbsolutePath}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("/docs");
        }

        [Fact]
        public void UriPartAbsoluteUri()
        {
            var uri = @"https://octopus.com/docs";
            var result = Evaluate(@"#{foo | UriPart AbsoluteUri}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("https://octopus.com/docs");
        }

        [Fact]
        public void UriPartAuthority()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart Authority}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("octopus.com");
        }

        [Fact]
        public void UriPartDnsSafeHost()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart DnsSafeHost}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("octopus.com");
        }

        [Fact]
        public void UriPartFragment()
        {
            var uri = @"https://octopus.com/docs/deployment-process/variables/variable-substitutions#VariableSubstitutionSyntax-Conditionalsconditionals";
            var result = Evaluate(@"#{foo | UriPart Fragment}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("#VariableSubstitutionSyntax-Conditionalsconditionals");
        }

        [Fact]
        public void UriPartHost()
        {
            var uri = @"https://octopus.com/docs/deployment-process/variables/variable-substitutions";
            var result = Evaluate(@"#{foo | UriPart Host}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("octopus.com");
        }

        [Fact]
        public void UriPartHostNameType()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart HostNameType}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("Dns");
        }

        [Fact]
        public void UriPartIsAbsoluteUri()
        {
            var uri = @"https://octopus.com/docs/deployment-process/variables/variable-substitutions";
            var result = Evaluate(@"#{foo | UriPart IsAbsoluteUri}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("True");
        }

        [Fact]
        public void UriPartIsDefaultPort()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart IsDefaultPort}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("True");
        }

        [Fact]
        public void UriPartIsFile()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart IsFile}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("False");
        }

        [Fact]
        public void UriPartIsLoopback()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart IsLoopback}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("False");
        }

        [Fact]
        public void UriPartIsUnc()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart IsUnc}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("False");
        }

        [Fact]
        public void UriPartLocalPath()
        {
            var uri = @"https://octopus.com/docs/deployment-process/variables/variable-substitutions";
            var result = Evaluate(@"#{foo | UriPart LocalPath}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("/docs/deployment-process/variables/variable-substitutions");
        }

        [Fact]
        public void UriPartPathAndQuery()
        {
            var uri = @"https://octopus.com/docs/deployment-process/variables/variable-substitutions";
            var result = Evaluate(@"#{foo | UriPart PathAndQuery}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("/docs/deployment-process/variables/variable-substitutions");
        }

        [Fact]
        public void UriPartPort()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart Port}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("443");
        }

        [Fact]
        public void UriPartQuery()
        {
            var uri = @"https://octopus.com/docs?filter=substitution";
            var result = Evaluate(@"#{foo | UriPart Query}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("?filter=substitution");
        }

        [Fact]
        public void UriPartScheme()
        {
            var uri = @"https://octopus.com";
            var result = Evaluate(@"#{foo | UriPart Scheme}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("https");
        }

        [Fact]
        public void UriPartUserInfo()
        {
            var uri = @"https://username:password@octopus.com";
            var result = Evaluate(@"#{foo | UriPart UserInfo}", new Dictionary<string, string> { { "foo", uri } });
            result.Should().Be("username:password");
        }
    }
}
