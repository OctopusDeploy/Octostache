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
            // This template is using the internal syntax, full form syntax, without omitting any spaces or #{else} statements.
            // For example, #{if test}#{/if} has a full syntax of #{if test}#{else}#{/if}
            var template = @"
                #{var}
                #{filter | Format #{option}}
                #{nestedParent[#{nestedChild}]}
                #{if ifvar}#{if ifnested}#{else}debug#{/if}#{else}#{/if}
                #{if comparison != ""value"" }true#{else}#{/if}
                #{each item in List}
                   #{item.Address}
                #{/each}
                ##{ignored}
            ";

            TemplateParser.TryParseTemplate(template, out var parsedTemplate, out string error).Should().BeTrue();
            string.IsNullOrEmpty(error).Should().BeTrue();
            parsedTemplate.Should().NotBeNull();
            parsedTemplate.ToString().Should().Be(template);
        }
    }
}