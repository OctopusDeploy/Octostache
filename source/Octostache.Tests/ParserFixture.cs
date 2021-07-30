using System;
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

        [Fact]
        public void Test()
        {
            var template = @"{
    ""name"": ""myservice"",
    ""containers"": [
        {
            ""containerName"": ""nginx"",
            ""container"": {
                ""imageName"": ""nginx"",
                ""imageTag"": ""#{Octopus.Action.Package[containers[0].container].PackageVersion}"",
                ""feed"": {
                    ""url"": ""#{Octopus.Action.Package[containers[0].container].Registry}""
                }
            },
            ""memoryLimitHard"": 0
        },
        {
            ""containerName"": ""busybox"",
            ""container"": {
                ""imageName"": ""busybox"",
                ""imageTag"": ""#{Octopus.Action.Package[containers[1].container].PackageVersion}"",
                ""feed"": {
                    ""url"": ""#{Octopus.Action.Package[containers[1].container].Registry}""
                }
            },
            ""memoryLimitHard"": 0
        }
    ],
    ""task"": {
        ""taskName"": """"
    },
    ""networkConfiguration"": {
        ""securityGroupId"": ""foo"",
        ""subnetId"": ""foo"",
        ""autoAssignPublicIp"": true
    },
    ""desiredCount"": 1,
    ""additionalTags"": []
}";

            // This represents what we do in calamari when processing inputs for a step package in JsonEscapeAllVariablesInOurInputs:
            // https://github.com/OctopusDeploy/Calamari/blob/cbc46c67aa3b94e7d4b06a69c24694fb50bcb30e/source/Calamari/LaunchTools/NodeExecutor.cs#L70
            var result = TemplateParser.ParseTemplate(template);

            result.Tokens.Should().NotBeEmpty();
        }
    }
}