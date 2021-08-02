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
                #{Octopus.Action[].Package[containers[1].container].PackageVersion}
                #{Octopus.Action[].Package[array[foo].containers[1].container].PackageVersion}
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
                "[containers[1].container]",
                "[array[foo].containers[1].container]"
            });

            result.Should().NotContain("ignored");
            result.Should().NotContain("item");
            result.Should().NotContain("item.Address");
        }
    }
}