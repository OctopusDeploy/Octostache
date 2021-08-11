using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
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

            var result = TemplateParser.ParseTemplateAndGetArgumentNames(template, true);
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
            // This test verifies that it is possible to convert a parsed template ToString
            // And then find individual tokens within the template by calling ToString on each one individually
            
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

            TemplateParser.TryParseTemplate(template, out var parsedTemplate, out string error).Should().BeTrue();
            string.IsNullOrEmpty(error).Should().BeTrue();
            parsedTemplate.Should().NotBeNull();
            string templateConvertedBackToString = parsedTemplate.ToString();
            foreach (var templateToken in parsedTemplate.Tokens)
            {
                if (!string.IsNullOrWhiteSpace(templateToken.ToString()))
                    templateConvertedBackToString = templateConvertedBackToString.Replace(templateToken.ToString(),  "");
            }

            templateConvertedBackToString = templateConvertedBackToString.Replace("\r\n", "");
            templateConvertedBackToString.Trim().Should().BeEmpty();
        }
    }
}