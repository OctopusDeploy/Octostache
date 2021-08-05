using System.Collections.Generic;
using Xunit;
using FluentAssertions;

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
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"},
                    {"Octopus.Action[].Name", "Blank"},
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
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package A].TargetRoles", "a,b"},
                    {"Octopus.Action[Package B].Name", "B"},
                    {"Octopus.Action[Package B].TargetRoles", "c"},
                    {"Octopus.Action[].Name", "Z"},
                    {"Octopus.Action[].TargetRoles", "y"}
                });

            result.Should().Be("AaAbBcZy");
        }

        [Fact]
        public void RecursiveIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    {"PackageA_Name", "A"},
                    {"PackageB_Name", "B"},
                    {"PackageC_Name", "C"},
                    {"Octopus.Action[Package A].Name", "#{PackageA_Name}"},
                    {"Octopus.Action[Package B].Name", "#{PackageB_Name}"},
                    {"Octopus.Action[].Name", "#{PackageC_Name}"},
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
                        {"Octopus.Action[Action 1].StepName", "Step 1"},
                        {"Octopus.Action[Action 2].StepName", "Step 2"},
                        {"Octopus.Step[Step 1].Details", "Step 1 Details"},
                        {"Octopus.Step[Step 2].Details", "Step 2 Details"},
                        {"Octopus.Step[Step 1].Status", "Skipped"},
                        {"Octopus.Step[Step 2].Status", "Running"},
                    });

            result.Should().Be("Step 2 Details");
        }

        [Fact]
        public void IterationSpecialVariablesAreSupported()
        {
            var result = Evaluate(@"#{each a in Numbers}#{a} First:#{Octopus.Template.Each.First} Last:#{Octopus.Template.Each.Last} Index:#{Octopus.Template.Each.Index}, #{/each}",
                new Dictionary<string, string>
                {
                    {"Numbers", "A,B,C"},
                });

            result.Should().Be("A First:True Last:False Index:0, B First:False Last:False Index:1, C First:False Last:True Index:2, ");
        }

        [Fact]
        public void NestedIndexIterationIsSupported()
        {
            var result =
                Evaluate(
                         "#{each a in Octopus.Action.Package}#{a}: #{a.Name} #{/each}",
                         new Dictionary<string, string>
                         {
                             { "Octopus.Action.Package[container[0]].Name", "A" },
                             { "Octopus.Action.Package[container[1]].Name", "B" },
                             { "Octopus.Action.Package[container[2]].Name", "C" },
                         });

            result.Should().Be("container[0]: A container[1]: B container[2]: C ");
        }
    }
}