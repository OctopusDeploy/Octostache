using System.Collections.Generic;
using NUnit.Framework;

namespace Octostache.Tests
{
    [TestFixture]
    public class IterationFixture : BaseFixture
    {
        [Test]
        public void IterationOverAnEmptyCollectionIsFine()
        {
            var result = Evaluate("Ok#{each nothing in missing}#{nothing}#{/each}", new Dictionary<string, string>());

            Assert.AreEqual("Ok", result);
        }

        [Test]
        public void SimpleIterationIsSupported()
        {
            var result = Evaluate(
                "#{each a in Octopus.Action}#{a}-#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package B].Name", "B"},
                });

            Assert.AreEqual("Package A-APackage B-B", result);
        }

        [Test]
        public void NestedIterationIsSupported()
        {
            var result = Evaluate(
                "#{each a in Octopus.Action}#{each tr in a.TargetRoles}#{a.Name}#{tr}#{/each}#{/each}",
                new Dictionary<string, string>
                {
                    {"Octopus.Action[Package A].Name", "A"},
                    {"Octopus.Action[Package A].TargetRoles", "a,b"},
                    {"Octopus.Action[Package B].Name", "B"},
                    {"Octopus.Action[Package B].TargetRoles", "c"}
                });

            Assert.AreEqual("AaAbBc", result);
        }

        [Test]
        public void RecursiveIterationIsSupported()
        {
            var result = Evaluate("#{each a in Octopus.Action}#{a.Name}#{/each}",
                new Dictionary<string, string>
                {
                    {"PackageA_Name", "A"},
                    {"PackageB_Name", "B"},
                    {"Octopus.Action[Package A].Name", "#{PackageA_Name}"},
                    {"Octopus.Action[Package B].Name", "#{PackageB_Name}"},
                });

            Assert.AreEqual("AB", result);
        }

        [Test]
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

            Assert.AreEqual("Step 2 Details", result);
        }

        [Test]
        public void IterationSpecialVariablesAreSupported()
        {
            var result = Evaluate(@"#{each a in Numbers}#{a} First:#{Octopus.Template.Each.First} Last:#{Octopus.Template.Each.Last} Index:#{Octopus.Template.Each.Index}, #{/each}",
                new Dictionary<string, string>
                {
                    {"Numbers", "A,B,C"},
                });

            Assert.AreEqual("A First:True Last:False Index:0, B First:False Last:False Index:1, C First:False Last:True Index:2, ", result);
        }
    }
}