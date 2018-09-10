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
                "List"
            });

            result.Should().NotContain("ignored");
            result.Should().NotContain("item");
            result.Should().NotContain("item.Address");
        }
    }
}