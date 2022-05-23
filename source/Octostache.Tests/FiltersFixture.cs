using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
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
            var result = Evaluate("#{Foo | ToBazooka 6}", new Dictionary<string, string> { { "Foo", "Abc" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
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
        [InlineData("a\n\r\nb", "a\n\n\r\nb")]
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
        [InlineData("\r\n", "\r\n")]
        [InlineData("\r\n\r\n", "\r\n\r\n")]
        [InlineData("a\nb", "a\nb")]
        [InlineData("a \nb", "a\nb")] // white space before a newline: cannot be escaped within single quotes
        [InlineData("a\n b", "a\nb")] // white space after a newline: cannot be escaped within single quotes
        [InlineData("a\n\nb", "a\n\nb")]
        [InlineData("a\r\nb", "a\r\nb")]
        [InlineData("a\r\n\nb", "a\r\n\nb")]
        [InlineData("a \n\nb", "a\n\nb")]
        [InlineData("a\n\n\n\n\r\nb", "a\n\n\n\n\r\nb")]
        [InlineData("this contains six spaces\nand one line break", "this contains six spaces\nand one line break")]
        public void YamlSingleQuotedStringsCanRoundTripWithSideEffects(string input, string expected)
        {
            var yaml = Evaluate("Key: '#{Input | YamlSingleQuoteEscape}'", new Dictionary<string, string> { { "Input", input } });

            var doc = new DeserializerBuilder()
                      .Build()
                      .Deserialize<TestDocument>(yaml);

            // Yamldotnet normalises \r\n in single quoted scalars to \n.
            var normalisedExpected = expected.Replace("\r\n", "\n");

            doc.Key.Should().Be(normalisedExpected);
        }

        [Theory]
        [InlineData(" ", "\\ ")]
        [InlineData(":", "\\:")]
        [InlineData("=", "\\=")]
        [InlineData("\\", "\\\\")]
        [InlineData("\r", "\\r")]
        [InlineData("\n", "\\n")]
        [InlineData("\t", "\\t")]
        [InlineData(" a \n b ", "\\ a\\ \\n\\ b\\ ")]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [InlineData("0123456789", "0123456789")]
        [InlineData("我叫章鱼", "\\u6211\\u53eb\\u7ae0\\u9c7c")]
        [InlineData("÷ü", "÷ü")]
        public void PropertiesKeyIsEscaped(string input, string expected)
        {
            var result = Evaluate("#{Foo | PropertiesKeyEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(":", ":")]
        [InlineData("=", "=")]
        [InlineData(" ", "\\ ")]
        [InlineData("a ", "a ")]
        [InlineData("\\", "\\\\")]
        [InlineData("\r", "\\r")]
        [InlineData("\n", "\\n")]
        [InlineData("\t", "\\t")]
        [InlineData(" a \n b ", "\\ a \\n b ")]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [InlineData("0123456789", "0123456789")]
        [InlineData("我叫章鱼", "\\u6211\\u53eb\\u7ae0\\u9c7c")]
        [InlineData("÷ü", "÷ü")]
        public void PropertiesValueIsEscaped(string input, string expected)
        {
            var result = Evaluate("#{Foo | PropertiesValueEscape}", new Dictionary<string, string> { { "Foo", input } });
            result.Should().Be(expected);
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
            var dictionary = new Dictionary<string, string>
            {
                {
                    "Foo",
                    @"|Header1|Header2|
|-|-|
|Cell1|Cell2|"
                }
            };
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
            var dict = new Dictionary<string, string> { { "Cash", "23.4" } };

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
            var dict = new Dictionary<string, string> { { "MyDate", "2030/05/22 09:05:00" } };
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

            var result = Evaluate("#{Invalid | Format yyyy}", dict)
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{Invalid | Format yyyy}");
        }

        [Fact]
        public void NowDateReturnsNow()
        {
            var result = Evaluate("#{ | NowDate}", new Dictionary<string, string>());
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
            var result = Evaluate("#{String | tobase64}", dict);
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
            var result = Evaluate("#{foo | Replace #{regex}#{replacement}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", "abc" },
                                      { "regex", "b" },
                                      { "replacement", "x" }
                                  });
            result.Should().Be(@"axc");
        }

        [Fact]
        public void ReplaceHandlesDoubleQuotesViaNestedSubsitution()
        {
            var result = Evaluate("#{foo | Replace #{regex}#{replacement}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", @"a""b" },
                                      { "regex", @"a""" },
                                      { "replacement", @"""c" }
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
            var result = Evaluate(@"#{foo | Substring}", new Dictionary<string, string> { { "foo", "ababa" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring}");
        }

        [Fact]
        public void SubstringWithTooManyArgumentsDoesNothing()
        {
            var result = Evaluate(@"#{foo | Substring 1 2 3}", new Dictionary<string, string> { { "foo", "ababa" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring 1 2 3}");
        }

        [Fact]
        public void SubstringWithOnlyLength()
        {
            var result = Evaluate(@"#{foo | Substring 7}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("Octopus");
        }

        [Fact]
        public void SubstringWithStartAndLength()
        {
            var result = Evaluate(@"#{foo | Substring 8 6}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("Deploy");
        }

        [Fact]
        public void SubstringHandlesNonNumericLength()
        {
            var result = Evaluate(@"#{foo | Substring a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring a}");
        }

        [Fact]
        public void SubstringHandlesNonNumericLengthWithStart()
        {
            var result = Evaluate(@"#{foo | Substring 0 a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring 0 a}");
        }

        [Fact]
        public void SubstringHandlesLengthIndexOutOfRange()
        {
            var result = Evaluate(@"#{foo | Substring 20}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring 20}");
        }

        [Fact]
        public void SubstringHandlesStartAndLengthIndexOutOfRange()
        {
            var result = Evaluate(@"#{foo | Substring 8 7}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring 8 7}");
        }

        [Fact]
        public void SubstringHandlesNegativeValueForLength()
        {
            var result = Evaluate(@"#{foo | Substring -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring -1}");
        }

        [Fact]
        public void SubstringHandlesNegativeStartAndLength()
        {
            var result = Evaluate(@"#{foo | Substring 0 -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Substring 0 -1}");
        }

        [Fact]
        public void TruncateDoesNothing()
        {
            var result = Evaluate(@"#{foo | Truncate}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Truncate}");
        }

        [Fact]
        public void TruncateDoesNothingWithLengthGreaterThanArgumentLength()
        {
            var result = Evaluate(@"#{foo | Truncate 50}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("Octopus Deploy");
        }

        [Fact]
        public void TruncateHandlesNonNumericLength()
        {
            var result = Evaluate(@"#{foo | Truncate a}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Truncate a}");
        }

        [Fact]
        public void TruncateHandlesNegativeLength()
        {
            var result = Evaluate(@"#{foo | Truncate -1}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
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
            var result = Evaluate(@"#{foo | Trim Both}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{foo | Trim Both}");
        }

        [Fact]
        public void IndentingAnEmptyStringHasNoEffect()
        {
            var result = Evaluate(@"#{foo | Indent}", new Dictionary<string, string> { { "foo", string.Empty } });
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void IndentingDefaultsToFourSpaces()
        {
            var result = Evaluate(@"#{foo | Indent}", new Dictionary<string, string> { { "foo", "Octopus Deploy" } });
            result.Should().Be("    Octopus Deploy");
        }

        [Fact]
        public void IndentingDefaultsToFourSpacesAcrossMultipleLines()
        {
            var result = Evaluate(@"#{foo | Indent}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("    Octopus Deploy\n    Octopus Deploy\n    Octopus Deploy");
        }

        [Fact]
        public void IndentWithCustomSizeAffectsEachLine()
        {
            var result = Evaluate(@"#{foo | Indent 2}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("  Octopus Deploy\n  Octopus Deploy\n  Octopus Deploy");
        }

        [Fact]
        public void IndentOnSubsequentLinesOnly()
        {
            var result = Evaluate(@"#{foo | Indent /3}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("Octopus Deploy\n   Octopus Deploy\n   Octopus Deploy");
        }

        [Fact]
        public void IndentSizeDifferentOnSubsequentLines()
        {
            var result = Evaluate(@"#{foo | Indent 3/2}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("   Octopus Deploy\n  Octopus Deploy\n  Octopus Deploy");
        }

        [Fact]
        public void IndentWithCustomValue()
        {
            var result = Evaluate(@"#{foo | Indent //}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("//Octopus Deploy\n//Octopus Deploy\n//Octopus Deploy");
        }

        [Fact]
        public void IndentWithDifferentOnSubsequentLines()
        {
            var result = Evaluate("#{foo | Indent \"/* \" \" * \"}", new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } });
            result.Should().Be("/* Octopus Deploy\n * Octopus Deploy\n * Octopus Deploy");
        }

        [Fact]
        public void IndentHasMaximumSizeOf255()
        {
            var d = new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } };
            var result = Evaluate(@"#{foo | Indent 255}", d);
            result.Should().Be(string.Join("\n", Enumerable.Repeat(new string(' ', 255) + "Octopus Deploy", 3)));

            var outOfBoundsResult = Evaluate(@"#{foo | Indent 256}", d);
            outOfBoundsResult.Should().Be(string.Join("\n", Enumerable.Repeat("256Octopus Deploy", 3)));
        }

        [Fact]
        public void IndentMaximumAppliesOnSubsequent()
        {
            var d = new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } };
            var result = Evaluate(@"#{foo | Indent /255}", d);
            result.Should().Be(string.Join("\n", Enumerable.Repeat(new string(' ', 255) + "Octopus Deploy", 3)).TrimStart());

            var outOfBoundsResult = Evaluate(@"#{foo | Indent /256}", d);
            outOfBoundsResult.Should().Be(string.Join("\n", Enumerable.Repeat("/256Octopus Deploy", 3)));
        }

        [Fact]
        public void IndentMaximumAppliesOnInitial()
        {
            var d = new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } };
            var result = Evaluate(@"#{foo | Indent 255/3}", d);
            result.Should().Be(new string(' ', 252) + string.Join("\n", Enumerable.Repeat("   Octopus Deploy", 3)));

            var outOfBoundsResult = Evaluate(@"#{foo | Indent 256/3}", d);
            outOfBoundsResult.Should().Be(string.Join("\n", Enumerable.Repeat("256/3Octopus Deploy", 3)));
        }

        [Fact]
        public void IndentWithInvalidOptionsDoesNothing()
        {
            var template = "#{foo | Indent abc 123 def}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "foo" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Fact]
        public void IndentWithNoArgumentDoesNothing()
        {
            var template = "#{ | Indent}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "foo" } });
            result.Should().Be(template);
        }

        [Fact]
        public void IndentWithHashCharacter()
        {
            // The hash character requires special quoting in filter options
            var template = "#{ foo | Indent \\\"# \\\"}";
            var d = new Dictionary<string, string> { { "foo", "Octopus Deploy\nOctopus Deploy\nOctopus Deploy" } };
            var result = Evaluate(template, d);
            result.Should().Be(string.Join("\n", Enumerable.Repeat("# Octopus Deploy", 3)));
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

        [Theory]
        [InlineData("abc def", "Match abc", "true", "Substring in string")]
        [InlineData("abc def", "Match cba", "false", "Substring not in string")]
        [InlineData("abc def", "Match", "#{foo | Match}", "No argument provided")]
        [InlineData("abc def", "Match a b", "#{foo | Match \"a\" \"b\"}", "Too many arguments provided")]
        [InlineData("abc def", "Match ABC", "false", "Match is case sensitive")]
        [InlineData("abc def", @"Match ""abc def""", "true", "Match can handle spaces")]
        [InlineData("abc'def", @"Match ""abc'def""", "true", "Match can handle single quotes")]
        [InlineData("abc def", @"Match "".*""", "true", "Regex wildcard should match")]
        [InlineData("abc def1", @"Match ""[0-9]+""", "true", "Regex range should match")]
        [InlineData("abc(def", @"Match ""c\(d""", "true", "Escaping regex special character should match")]
        public void Match(string inputValue, string inputFilterExpression, string expectedOutput, string because)
        {
            var result = Evaluate($"#{{foo | {inputFilterExpression}}}", new Dictionary<string, string> { { "foo", inputValue } });
            result.Should().Be(expectedOutput, because);
        }

        [Fact]
        public void MatchWithVariableOptions()
        {
            var result = Evaluate("#{foo | Match #{regex}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", "abc def" },
                                      { "regex", "def" }
                                  });
            result.Should().Be("true", "Match can handle variable options");
        }

        [Theory]
        [InlineData("abc def", "StartsWith abc", "true", "Variable starts with argument")]
        [InlineData("abc def", "StartsWith bc", "false", "Variable does not start with argument")]
        [InlineData("abc def", "StartsWith", "#{foo | StartsWith}", "No argument provided")]
        [InlineData("abc def", "StartsWith a b", "#{foo | StartsWith \"a\" \"b\"}", "Too many arguments provided")]
        [InlineData("abc def", "StartsWith ABC", "false", "StartsWith is case sensitive")]
        [InlineData("abc def", @"StartsWith ""abc d""", "true", "StartsWith can handle spaces")]
        [InlineData("abc'def", @"StartsWith ""abc'""", "true", "StartsWith can handle single quotes")]
        public void StartsWith(string inputValue, string inputFilterExpression, string expectedOutput, string because)
        {
            var result = Evaluate($"#{{foo | {inputFilterExpression}}}", new Dictionary<string, string> { { "foo", inputValue } });
            result.Should().Be(expectedOutput, because);
        }

        [Fact]
        public void StartsWithWithVariableOptions()
        {
            var result = Evaluate("#{foo | StartsWith #{str}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", "abc def" },
                                      { "str", "abc" }
                                  });
            result.Should().Be("true", "StartsWith can handle variable options");
        }

        [Theory]
        [InlineData("abc def", "EndsWith def", "true", "Variable ends with argument")]
        [InlineData("abc def", "EndsWith de", "false", "Variable does not end with argument")]
        [InlineData("abc def", "EndsWith", "#{foo | EndsWith}", "No argument provided")]
        [InlineData("abc def", "EndsWith a b", "#{foo | EndsWith \"a\" \"b\"}", "Too many arguments provided")]
        [InlineData("abc def", "EndsWith DEF", "false", "EndsWith is case sensitive")]
        [InlineData("abc def", @"EndsWith ""c def""", "true", "EndsWith can handle spaces")]
        [InlineData("abc'def", @"EndsWith ""'def""", "true", "EndsWith can handle single quotes")]
        public void EndsWith(string inputValue, string inputFilterExpression, string expectedOutput, string because)
        {
            var result = Evaluate($"#{{foo | {inputFilterExpression}}}", new Dictionary<string, string> { { "foo", inputValue } });
            result.Should().Be(expectedOutput, because);
        }

        [Fact]
        public void EndsWithWithWithVariableOptions()
        {
            var result = Evaluate("#{foo | EndsWith #{str}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", "abc def" },
                                      { "str", "def" }
                                  });
            result.Should().Be("true", "EndsWith can handle variable options");
        }

        [Theory]
        [InlineData("abc def", "Contains de", "true", "Variable contains argument")]
        [InlineData("abc def", "Contains ed", "false", "Variable does not contain argument")]
        [InlineData("abc def", "Contains", "#{foo | Contains}", "No argument provided")]
        [InlineData("abc def", "Contains a b", "#{foo | Contains \"a\" \"b\"}", "Too many arguments provided")]
        [InlineData("abc def", "Contains ABC", "false", "Contains is case sensitive")]
        [InlineData("abc def", @"Contains ""c de""", "true", "Contains can handle spaces")]
        [InlineData("abc'def", @"Contains ""'de""", "true", "Contains can handle single quotes")]
        public void Contains(string inputValue, string inputFilterExpression, string expectedOutput, string because)
        {
            var result = Evaluate($"#{{foo | {inputFilterExpression}}}", new Dictionary<string, string> { { "foo", inputValue } });
            result.Should().Be(expectedOutput, because);
        }

        [Fact]
        public void ContainsWithWithVariableOptions()
        {
            var result = Evaluate("#{foo | Contains #{str}}",
                                  new Dictionary<string, string>
                                  {
                                      { "foo", "abc def" },
                                      { "str", "c d" }
                                  });
            result.Should().Be("true", "Contains can handle variable options");
        }

        [Fact]
        public void AppendDoesNotRequireAnArgument()
        {
            var result = Evaluate("#{|Append foo}", new Dictionary<string, string>());
            result.Should().Be("foo");
        }

        [Theory]
        [InlineData("#{foo | Append}")]
        [InlineData("#{ | Append}")]
        public void AppendRequiresAnOption(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } });
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("value", "value")]
        [InlineData("value1 value2", "value1value2")]
        [InlineData("value1 \" \" value2", "value1 value2")]
        [InlineData("\" \" #{foo} \" \" value", " bar value")]
        public void OptionsAreAppended(string options, string expectedToAppend)
        {
            var template = $"#{{foo | Append {options}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } });
            var expectedResult = $"bar{expectedToAppend}";
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void PrependDoesNotRequireAnArgument()
        {
            var result = Evaluate("#{|Prepend foo}", new Dictionary<string, string>());
            result.Should().Be("foo");
        }

        [Theory]
        [InlineData("#{foo | Prepend}")]
        [InlineData("#{ | Prepend}")]
        public void PrependRequiresAnOption(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } });
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("value", "value")]
        [InlineData("value1 value2", "value1value2")]
        [InlineData("value1 \" \" value2", "value1 value2")]
        [InlineData("\" \" #{foo} \" \" value", " bar value")]
        public void OptionsArePrepended(string options, string expectedToPrepend)
        {
            var template = $"#{{foo | Prepend {options}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } });
            var expectedResult = $"{expectedToPrepend}bar";
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("#{foo | Md5 -1}")]
        [InlineData("#{foo | Md5 0}")]
        [InlineData("#{foo | Md5 bar}")]
        [InlineData("#{foo | Md5 utf8 0}")]
        [InlineData("#{foo | Md5 0 utf8}")]
        [InlineData("#{foo | Md5 bar 12}")]
        [InlineData("#{foo | Md5 12 bar}")]
        [InlineData("#{foo | Md5 utf8 12 extra}")]
        [InlineData("#{foo | Md5 12 utf8 extra}")]
        [InlineData("#{foo | Md5 base64}")]
        [InlineData("#{ | Md5}")]
        public void Md5HashInvalidTemplate(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
        [InlineData("The quick brown fox jumps over the lazy dog", "9e107d9d372bb6826bd81d3542a419d6")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "e4d909c290d0fb1ca068ffaddf22cbd0")]
        public void Md5Hash(string input, string expectedHash)
        {
            var template = "#{foo | Md5}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "d41d8cd9", 4)]
        [InlineData("The quick brown fox jumps over the lazy dog", "9e107d", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "e4d909c290d0fb1ca068ffaddf22cbd0", 800)]
        public void Md5HashWithSize(string input, string expectedHash, int size)
        {
            var template = $"#{{foo | Md5 {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "d41d8cd98f00b204e9800998ecf8427e", "utf8")]
        [InlineData("", "d41d8cd98f00b204e9800998ecf8427e", "utf-8")]
        [InlineData("", "d41d8cd98f00b204e9800998ecf8427e", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog", "b0986ae6ee1eefee8a4a399090126837", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "e4d909c290d0fb1ca068ffaddf22cbd0", "utf8")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==", "9e107d9d372bb6826bd81d3542a419d6", "base64")]
        public void Md5HashWithEncoding(string input, string expectedHash, string encoding)
        {
            var template = $"#{{foo | Md5 {encoding}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "d41d8cd9", "utf8", 4)]
        [InlineData("", "d41d8cd98f", "utf-8", 5)]
        [InlineData("", "d41d8c", "unicode", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog", "b0986ae6ee1eefee8a4a399090126837", "unicode", 800)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "e4d9", "utf8", 2)]
        public void Md5HashWithEncodingAndSize(string input, string expectedHash, string encoding, int size)
        {
            var template = $"#{{foo | Md5 {encoding} {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);

            // Reversing the order of the options is supported
            var templateReversed = $"#{{foo | Md5 {size} {encoding}}}";
            var resultReversed = Evaluate(templateReversed, new Dictionary<string, string> { { "foo", input } });
            resultReversed.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("#{foo | Sha1 -1}")]
        [InlineData("#{foo | Sha1 0}")]
        [InlineData("#{foo | Sha1 bar}")]
        [InlineData("#{foo | Sha1 utf8 0}")]
        [InlineData("#{foo | Sha1 0 utf8}")]
        [InlineData("#{foo | Sha1 bar 12}")]
        [InlineData("#{foo | Sha1 12 bar}")]
        [InlineData("#{foo | Sha1 utf8 12 extra}")]
        [InlineData("#{foo | Sha1 12 utf8 extra}")]
        [InlineData("#{foo | Sha1 base64}")]
        [InlineData("#{ | Sha1}")]
        public void Sha1HashInvalidTemplate(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
        [InlineData("The quick brown fox jumps over the lazy dog", "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12")]
        [InlineData("The quick brown fox jumps over the lazy cog", "de9f2c7fd25e1b3afad3e85a0bd17d9b100db4b3")]
        public void Sha1Hash(string input, string expectedHash)
        {
            var template = "#{foo | Sha1}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "da39a3ee", 4)]
        [InlineData("The quick brown fox jumps over the lazy dog", "2fd4e1", 3)]
        [InlineData("The quick brown fox jumps over the lazy cog", "de9f2c7fd25e1b3afad3e85a0bd17d9b100db4b3", 800)]
        public void Sha1HashWithSize(string input, string expectedHash, int size)
        {
            var template = $"#{{foo | Sha1 {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709", "utf8")]
        [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709", "utf-8")]
        [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog", "bd136cb58899c93173c33a90dde95ead0d0cf6df", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy cog", "de9f2c7fd25e1b3afad3e85a0bd17d9b100db4b3", "utf8")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==", "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12", "base64")]
        public void Sha1HashWithEncoding(string input, string expectedHash, string encoding)
        {
            var template = $"#{{foo | Sha1 {encoding}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "da39a3ee", "utf8", 4)]
        [InlineData("", "da39a3ee5e", "utf-8", 5)]
        [InlineData("", "da39a3", "unicode", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog", "bd136cb58899c93173c33a90dde95ead0d0cf6df", "unicode", 800)]
        [InlineData("The quick brown fox jumps over the lazy cog", "de9f", "utf8", 2)]
        public void Sha1HashWithEncodingAndSize(string input, string expectedHash, string encoding, int size)
        {
            var template = $"#{{foo | Sha1 {encoding} {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);

            // Reversing the order of the options is supported
            var templateReversed = $"#{{foo | Sha1 {size} {encoding}}}";
            var resultReversed = Evaluate(templateReversed, new Dictionary<string, string> { { "foo", input } });
            resultReversed.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("#{foo | Sha256 -1}")]
        [InlineData("#{foo | Sha256 0}")]
        [InlineData("#{foo | Sha256 bar}")]
        [InlineData("#{foo | Sha256 utf8 0}")]
        [InlineData("#{foo | Sha256 0 utf8}")]
        [InlineData("#{foo | Sha256 bar 12}")]
        [InlineData("#{foo | Sha256 12 bar}")]
        [InlineData("#{foo | Sha256 utf8 12 extra}")]
        [InlineData("#{foo | Sha256 12 utf8 extra}")]
        [InlineData("#{foo | Sha256 base64}")]
        [InlineData("#{ | Sha256}")]
        public void Sha256HashInvalidTemplate(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData("The quick brown fox jumps over the lazy dog", "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ef537f25c895bfa782526529a9b63d97aa631564d5d789c2b765448c8635fb6c")]
        public void Sha256Hash(string input, string expectedHash)
        {
            var template = "#{foo | Sha256}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "e3b0c442", 4)]
        [InlineData("The quick brown fox jumps over the lazy dog", "d7a8fb", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ef537f25c895bfa782526529a9b63d97aa631564d5d789c2b765448c8635fb6c", 800)]
        public void Sha256HashWithSize(string input, string expectedHash, int size)
        {
            var template = $"#{{foo | Sha256 {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "utf8")]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "utf-8")]
        [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog", "3b5b0eac46c8f0c16fa1b9c187abc8379cc936f6508892969d49234c6c540e58", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ef537f25c895bfa782526529a9b63d97aa631564d5d789c2b765448c8635fb6c", "utf8")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==", "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592", "base64")]
        public void Sha256HashWithEncoding(string input, string expectedHash, string encoding)
        {
            var template = $"#{{foo | Sha256 {encoding}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "e3b0c442", "utf8", 4)]
        [InlineData("", "e3b0c44298", "utf-8", 5)]
        [InlineData("", "e3b0c4", "unicode", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog", "3b5b0eac46c8f0c16fa1b9c187abc8379cc936f6508892969d49234c6c540e58", "unicode", 800)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ef53", "utf8", 2)]
        public void Sha256HashWithEncodingAndSize(string input, string expectedHash, string encoding, int size)
        {
            var template = $"#{{foo | Sha256 {encoding} {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);

            // Reversing the order of the options is supported
            var templateReversed = $"#{{foo | Sha256 {size} {encoding}}}";
            var resultReversed = Evaluate(templateReversed, new Dictionary<string, string> { { "foo", input } });
            resultReversed.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("#{foo | Sha384 -1}")]
        [InlineData("#{foo | Sha384 0}")]
        [InlineData("#{foo | Sha384 bar}")]
        [InlineData("#{foo | Sha384 utf8 0}")]
        [InlineData("#{foo | Sha384 0 utf8}")]
        [InlineData("#{foo | Sha384 bar 12}")]
        [InlineData("#{foo | Sha384 12 bar}")]
        [InlineData("#{foo | Sha384 utf8 12 extra}")]
        [InlineData("#{foo | Sha384 12 utf8 extra}")]
        [InlineData("#{foo | Sha384 base64}")]
        [InlineData("#{ | Sha384}")]
        public void Sha384HashInvalidTemplate(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("", "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b")]
        [InlineData("The quick brown fox jumps over the lazy dog", "ca737f1014a48f4c0b6dd43cb177b0afd9e5169367544c494011e3317dbf9a509cb1e5dc1e85a941bbee3d7f2afbc9b1")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ed892481d8272ca6df370bf706e4d7bc1b5739fa2177aae6c50e946678718fc67a7af2819a021c2fc34e91bdb63409d7")]
        public void Sha384Hash(string input, string expectedHash)
        {
            var template = "#{foo | Sha384}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "38b060a7", 4)]
        [InlineData("The quick brown fox jumps over the lazy dog", "ca737f", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ed892481d8272ca6df370bf706e4d7bc1b5739fa2177aae6c50e946678718fc67a7af2819a021c2fc34e91bdb63409d7", 800)]
        public void Sha384HashWithSize(string input, string expectedHash, int size)
        {
            var template = $"#{{foo | Sha384 {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b", "utf8")]
        [InlineData("", "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b", "utf-8")]
        [InlineData("", "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog", "882ab99315b04348e3c7d0bcef46ec8ee7e6b418c5f1180f2dd2b0f86b7d26a6b080d25b180e5e96a7d9912abdd831dd", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ed892481d8272ca6df370bf706e4d7bc1b5739fa2177aae6c50e946678718fc67a7af2819a021c2fc34e91bdb63409d7", "utf8")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==", "ca737f1014a48f4c0b6dd43cb177b0afd9e5169367544c494011e3317dbf9a509cb1e5dc1e85a941bbee3d7f2afbc9b1", "base64")]
        public void Sha384HashWithEncoding(string input, string expectedHash, string encoding)
        {
            var template = $"#{{foo | Sha384 {encoding}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "38b060a7", "utf8", 4)]
        [InlineData("", "38b060a751", "utf-8", 5)]
        [InlineData("", "38b060", "unicode", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog", "882ab99315b04348e3c7d0bcef46ec8ee7e6b418c5f1180f2dd2b0f86b7d26a6b080d25b180e5e96a7d9912abdd831dd", "unicode", 800)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "ed89", "utf8", 2)]
        public void Sha384HashWithEncodingAndSize(string input, string expectedHash, string encoding, int size)
        {
            var template = $"#{{foo | Sha384 {encoding} {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);

            // Reversing the order of the options is supported
            var templateReversed = $"#{{foo | Sha384 {size} {encoding}}}";
            var resultReversed = Evaluate(templateReversed, new Dictionary<string, string> { { "foo", input } });
            resultReversed.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("#{foo | Sha512 -1}")]
        [InlineData("#{foo | Sha512 0}")]
        [InlineData("#{foo | Sha512 bar}")]
        [InlineData("#{foo | Sha512 utf8 0}")]
        [InlineData("#{foo | Sha512 0 utf8}")]
        [InlineData("#{foo | Sha512 bar 12}")]
        [InlineData("#{foo | Sha512 12 bar}")]
        [InlineData("#{foo | Sha512 utf8 12 extra}")]
        [InlineData("#{foo | Sha512 12 utf8 extra}")]
        [InlineData("#{foo | Sha512 base64}")]
        [InlineData("#{ | Sha512}")]
        public void Sha512HashInvalidTemplate(string template)
        {
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", "bar" } })
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be(template);
        }

        [Theory]
        [InlineData("", "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e")]
        [InlineData("The quick brown fox jumps over the lazy dog", "07e547d9586f6a73f73fbac0435ed76951218fb7d0c8d788a309d785436bbb642e93a252a954f23912547d1e8a3b5ed6e1bfd7097821233fa0538f3db854fee6")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "91ea1245f20d46ae9a037a989f54f1f790f0a47607eeb8a14d12890cea77a1bbc6c7ed9cf205e67b7f2b8fd4c7dfd3a7a8617e45f3c463d481c7e586c39ac1ed")]
        public void Sha512Hash(string input, string expectedHash)
        {
            var template = "#{foo | Sha512}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "cf83e135", 4)]
        [InlineData("The quick brown fox jumps over the lazy dog", "07e547", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "91ea1245f20d46ae9a037a989f54f1f790f0a47607eeb8a14d12890cea77a1bbc6c7ed9cf205e67b7f2b8fd4c7dfd3a7a8617e45f3c463d481c7e586c39ac1ed", 800)]
        public void Sha512HashWithSize(string input, string expectedHash, int size)
        {
            var template = $"#{{foo | Sha512 {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e", "utf8")]
        [InlineData("", "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e", "utf-8")]
        [InlineData("", "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog", "568a357630e3113c3932290749f4323eacee0c46ca02189c52d168ed35e0feeaa79e1a7ae725991df6e7d5e5c8f0877b8f51ab244ba7bde173033ad4de7c36de", "unicode")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "91ea1245f20d46ae9a037a989f54f1f790f0a47607eeb8a14d12890cea77a1bbc6c7ed9cf205e67b7f2b8fd4c7dfd3a7a8617e45f3c463d481c7e586c39ac1ed", "utf8")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==", "07e547d9586f6a73f73fbac0435ed76951218fb7d0c8d788a309d785436bbb642e93a252a954f23912547d1e8a3b5ed6e1bfd7097821233fa0538f3db854fee6", "base64")]
        public void Sha512HashWithEncoding(string input, string expectedHash, string encoding)
        {
            var template = $"#{{foo | Sha512 {encoding}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);
        }

        [Theory]
        [InlineData("", "cf83e135", "utf8", 4)]
        [InlineData("", "cf83e1357e", "utf-8", 5)]
        [InlineData("", "cf83e1", "unicode", 3)]
        [InlineData("The quick brown fox jumps over the lazy dog", "568a357630e3113c3932290749f4323eacee0c46ca02189c52d168ed35e0feeaa79e1a7ae725991df6e7d5e5c8f0877b8f51ab244ba7bde173033ad4de7c36de", "unicode", 800)]
        [InlineData("The quick brown fox jumps over the lazy dog.", "91ea", "utf8", 2)]
        public void Sha512HashWithEncodingAndSize(string input, string expectedHash, string encoding, int size)
        {
            var template = $"#{{foo | Sha512 {encoding} {size}}}";
            var result = Evaluate(template, new Dictionary<string, string> { { "foo", input } });
            result.Should().Be(expectedHash);

            // Reversing the order of the options is supported
            var templateReversed = $"#{{foo | Sha512 {size} {encoding}}}";
            var resultReversed = Evaluate(templateReversed, new Dictionary<string, string> { { "foo", input } });
            resultReversed.Should().Be(expectedHash);
        }

        class TestDocument
        {
            public string Key { get; set; }
        }
    }
}