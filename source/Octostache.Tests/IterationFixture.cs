using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class IterationFixture : BaseFixture
    {
        [Fact]
        public void IterationOverAnEmptyCollectionIsFine()
        {
            var result = Evaluate("Ok#{each nothing in missing}#{nothing}#{/each}", new Dictionary<string, string>());

            result.Should().Be("Ok");
        }

        [Fact]
        public void SimpleIterationIsSupported()
        {
            var result = Evaluate(
                "#{each a in Octopus.Action}#{a}-#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package B].Name", "B" },
                    { "Octopus.Action[].Name", "Blank" },
                });

            result.Should().Be("Package A-APackage B-B-Blank");
        }

        [Theory]
        [InlineData("#{ each a in Octopus.Action}#{a}-#{a.Name}#{/each}")]
        [InlineData("#{  each a in Octopus.Action}#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each  a in Octopus.Action}#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each a  in Octopus.Action}#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in  Octopus.Action}#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action }#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action  }#{a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action}#{ a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action}#{  a}-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action}#{a }-#{a.Name}#{/each}")]
        [InlineData("#{each a in Octopus.Action}#{a  }-#{a.Name}#{/each}")]
        public void IterationIgnoresWhitespacesCorrectly(string input)
        {
            var result = Evaluate(
                input,
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package B].Name", "B" },
                    { "Octopus.Action[].Name", "Blank" },
                });

            result.Should().Be("Package A-APackage B-B-Blank");
        }

        [Fact]
        public void NestedIterationIsSupported()
        {
            var result = Evaluate(
                "#{each a in Octopus.Action}#{each tr in a.TargetRoles}#{a.Name}#{tr}#{/each}#{/each}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action[Package A].Name", "A" },
                    { "Octopus.Action[Package A].TargetRoles", "a,b" },
                    { "Octopus.Action[Package B].Name", "B" },
                    { "Octopus.Action[Package B].TargetRoles", "c" },
                    { "Octopus.Action[].Name", "Z" },
                    { "Octopus.Action[].TargetRoles", "y" },
                });

            result.Should().Be("AaAbBcZy");
        }

        [Fact]
        public void RecursiveIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    { "PackageA_Name", "A" },
                    { "PackageB_Name", "B" },
                    { "PackageC_Name", "C" },
                    { "Octopus.Action[Package A].Name", "#{PackageA_Name}" },
                    { "Octopus.Action[Package B].Name", "#{PackageB_Name}" },
                    { "Octopus.Action[].Name", "#{PackageC_Name}" },
                });

            result.Should().Be("ABC");
        }

        [Fact]
        public void ScopedSymbolIndexerInIterationIsSupported()
        {
            var result =
                Evaluate(
                    "#{each action in Octopus.Action}#{if Octopus.Step[#{action.StepName}].Status != \"Skipped\"}#{Octopus.Step[#{action.StepName}].Details}#{/if}#{/each}",
                    new Dictionary<string, string>
                    {
                        { "Octopus.Action[Action 1].StepName", "Step 1" },
                        { "Octopus.Action[Action 2].StepName", "Step 2" },
                        { "Octopus.Step[Step 1].Details", "Step 1 Details" },
                        { "Octopus.Step[Step 2].Details", "Step 2 Details" },
                        { "Octopus.Step[Step 1].Status", "Skipped" },
                        { "Octopus.Step[Step 2].Status", "Running" },
                    });

            result.Should().Be("Step 2 Details");
        }

        [Fact]
        public void IterationSpecialVariablesAreSupported()
        {
            var result = Evaluate(@"#{each a in Numbers}#{a} First:#{Octopus.Template.Each.First} Last:#{Octopus.Template.Each.Last} Index:#{Octopus.Template.Each.Index}, #{/each}",
                new Dictionary<string, string>
                {
                    { "Numbers", "A,B,C" },
                });

            result.Should().Be("A First:True Last:False Index:0, B First:False Last:False Index:1, C First:False Last:True Index:2, ");
        }

        [Fact]
        public void NestedIndexIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action.Package}#{a}: #{a.Name} #{/each}",
                new Dictionary<string, string>
                {
                    { "Octopus.Action.Package[container[0]].Name", "A" },
                    { "Octopus.Action.Package[container[1]].Name", "B" },
                    { "Octopus.Action.Package[container[2]].Name", "C" },
                });

            result.Should().Be("container[0]: A container[1]: B container[2]: C ");
        }

        [Fact]
        public void KeyIsAvailableWhenIndexedCollectionHasDirectValues()
        {
            var result = Evaluate("#{each x in Simple}[#{x.Key}]#{/each}",
                new Dictionary<string, string>
                {
                    { "Simple[a]", "5" },
                    { "Simple[b]", "7" },
                });

            result.Should().Be("[a][b]");
        }

        [Fact]
        public void KeyIsAvailableWhenIndexedCollectionHasOnlySubProperties()
        {
            var result = Evaluate("#{each x in NoDirect}[#{x.Key}]#{/each}",
                new Dictionary<string, string>
                {
                    { "NoDirect[a].Name", "Alpha" },
                    { "NoDirect[b].Name", "Beta" },
                });

            result.Should().Be("[a][b]");
        }

        [Fact]
        public void KeyIsAvailableAlongsideSubPropertiesForCertificateShapedCollections()
        {
            var result = Evaluate("#{each x in Cert}[#{x.Key}=#{x.Thumbprint}]#{/each}",
                new Dictionary<string, string>
                {
                    { "Cert[a]", "Certificate-1" },
                    { "Cert[a].Thumbprint", "AAA" },
                    { "Cert[b]", "Certificate-2" },
                    { "Cert[b].Thumbprint", "BBB" },
                });

            result.Should().Be("[a=AAA][b=BBB]");
        }

        [Theory]
        [InlineData("#{each x in Simple}[#{x}]#{/each}", "[5][7]")]
        [InlineData("#{each x in NoDirect}[#{x}]#{/each}", "[a][b]")]
        [InlineData("#{each x in Cert}[#{x}]#{/each}", "[Certificate-1][Certificate-2]")]
        public void BareIterationVariableIsUnaffectedByTheKeyBinding(string template, string expected)
        {
            var result = Evaluate(template,
                new Dictionary<string, string>
                {
                    { "Simple[a]", "5" },
                    { "Simple[b]", "7" },
                    { "NoDirect[a].Name", "Alpha" },
                    { "NoDirect[b].Name", "Beta" },
                    { "Cert[a]", "Certificate-1" },
                    { "Cert[a].Thumbprint", "AAA" },
                    { "Cert[b]", "Certificate-2" },
                    { "Cert[b].Thumbprint", "BBB" },
                });

            result.Should().Be(expected);
        }

        [Fact]
        public void UserDefinedKeyVariableOverridesTheSynthesizedKey()
        {
            var result = Evaluate("#{each x in Coll}[#{x.Key}]#{/each} #{Coll[a].Key}",
                new Dictionary<string, string>
                {
                    { "Coll[a]", "val-a" },
                    { "Coll[a].Key", "user-supplied" },
                });

            result.Should().Be("[user-supplied] user-supplied");
        }

        [Fact]
        public void UserDefinedKeyVariableOverridesTheSynthesizedKeyRegardlessOfDeclarationOrder()
        {
            var result = Evaluate("#{each x in Coll}[#{x.Key}]#{/each}",
                new Dictionary<string, string>
                {
                    { "Coll[a].Key", "user-supplied" },
                    { "Coll[a].Name", "Alpha" },
                });

            result.Should().Be("[user-supplied]");
        }

        [Fact]
        public void KeyAndValueRemainAvailableForJsonBackedCollections()
        {
            var result = Evaluate("#{each x in Json}[#{x.Key}=#{x.Value}]#{/each}",
                new Dictionary<string, string>
                {
                    { "Json", "{\"a\": \"AAA\", \"b\": \"BBB\"}" },
                });

            result.Should().Be("[a=AAA][b=BBB]");
        }
    }
}
