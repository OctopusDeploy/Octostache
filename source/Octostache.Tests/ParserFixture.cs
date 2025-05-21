using System;
using System.Diagnostics;
using FluentAssertions;
using Octostache.Templates;
using Xunit;

namespace Octostache.Tests
{
    public class ParserFixture : BaseFixture
    {
        [Fact]
        public void AListOfVariablesCanBeExtracted()
        {
            var template = @"
                #{var}
                #{filter | Format #{option}}
                #{nestedParent[#{nestedChild}]}
                #{if ifvar}#{unless ifnested}debug#{/unless}#{/if}
                #{if comparison != ""value""}true#{/if}
                #{each item in List}
                   #{item.Address}
                #{/each}
                ##{ignored}
            ";

            var result = TemplateParser.ParseTemplateAndGetArgumentNames(template);
            result.Should().Contain(new[]
            {
                "var",
                "filter",
                "option",
                "nestedParent",
                "nestedChild",
                "ifvar",
                "ifnested",
                "comparison",
                "List",
            });

            result.Should().NotContain("ignored");
            result.Should().NotContain("item");
            result.Should().NotContain("item.Address");
        }

        [Fact]
        public void ParsedTemplateIsConvertibleBackToString()
        {
            // This test verifies that it is possible to convert a parsed template to a string
            // and then find individual tokens within the template by calling ToString on each token individually

            var template = @"
                #{var}
                #{filter | Format #{option}}
                #{nestedParent[#{nestedChild}]}
                #{if ifvar}#{if ifnested}#{else}debug#{/if}#{/if}
                #{if comparison != ""value"" }true#{/if}
                #{each item in List}
                   #{item.Address}
                #{/each}
                ##{ignored}
                #{MyVar | Match ""a b""}
                #{MyVar | StartsWith Ab}
                #{MyVar | Match ""ab[0-9]+""}
                #{MyVar | Match #{pattern}}
                #{MyVar | Contains "" b(""}
                #{ | NowDateUtc}
                #{MyVar | UriPart IsFile}
            ";

            TemplateParser.TryParseTemplate(template, out var parsedTemplate, out _);
            parsedTemplate.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException - Asserted above
            parsedTemplate.ToString().Should().NotBeEmpty();

            // We convert the template back to the string representation and then remove individual parsed expressions until there is nothing left
            // The purpose is to verify that both methods (on template and tokens) are deterministic and produce equivalent string representations
            var templateConvertedBackToString = parsedTemplate.ToString();
            foreach (var templateToken in parsedTemplate.Tokens)
            {
                if (!string.IsNullOrWhiteSpace(templateToken.ToString()))
                    templateConvertedBackToString = templateConvertedBackToString.Replace(templateToken.ToString(), "");
            }

            templateConvertedBackToString = templateConvertedBackToString.Replace("\r\n", "");
            templateConvertedBackToString.Trim().Should().BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        protected void TryParseWithHaltOnError(bool haltOnError)
        {
            var template = @"
                #{var}
                #{if ifvar}#{if ifnested}#{else}debug#{/if}#{/if}
                missing start tag#{/if}
                ##{ignored}
                #{MyVar | Match ""a b""}
            ";

            TemplateParser.TryParseTemplate(template, out var parsedTemplate1, out var error1, haltOnError);
            TemplateParser.TryParseTemplate(template, out var parsedTemplate2, out var error2, !haltOnError);

            if (haltOnError)
            {
                error1.Should().NotBeNullOrEmpty();
                parsedTemplate1.Should().BeNull();
                error2.Should().BeNullOrEmpty();
                parsedTemplate2.Should().NotBeNull();
            }
            else
            {
                error1.Should().BeNullOrEmpty();
                parsedTemplate1.Should().NotBeNull();
                error2.Should().NotBeNullOrEmpty();
                parsedTemplate2.Should().BeNull();
            }
        }

        [Fact]
        protected void EvaluateLotsOfTimesWithSet()
        {
            var sw = new Stopwatch();
            var dictionary = new VariableDictionary();
            for (var x = 0; x < 5_000; x++)
                dictionary.Add($"Octopus.Step[{x.ToString("")}].Action[{x.ToString("")}]", "Value");

            sw.Start();
            for (var x = 0; x < 100; x++)
            {
                // The set effectively re-sets the binding, so we can test the cache of the path parsing.
                dictionary.Set("a", "a");
                dictionary.Evaluate("#{foo}");
            }

            sw.Stop();
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(12));
        }
    }
}
