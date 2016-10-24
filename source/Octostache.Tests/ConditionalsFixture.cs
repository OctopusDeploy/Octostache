using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Octostache.Tests
{
    [TestFixture]
    public class ConditionalsFixture :BaseFixture
    {

        [Test]
        public void ConditionalIsSupported()
        {
            var result = Evaluate("#{if Truthy}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    {"Result", "result"},
                    {"Truthy", "true"},
                });

            Assert.AreEqual("result", result);
        }

        [Test]
        public void ConditionalToOtherDictValueIsSupported()
        {
            var result = Evaluate("#{if Octopus == Compare}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    {"Result", "result"},
                    {"Octopus", "octopus"},
                    {"Compare", "octopus"}
                });

            Assert.AreEqual("result", result);
        }

        [Test]
        public void ConditionalToStringIsSupported()
        {
            var result = Evaluate("#{if Octopus == \"octopus\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    {"Result", "result"},
                    {"Octopus", "octopus"},
                });

            Assert.AreEqual("result", result);
        }

        [Test]
        public void ConditionalNegationIsSupported()
        {
            var result = Evaluate("#{if Octopus != \"software\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    {"Result", "result"},
                    {"software", "something else"},
                });

            Assert.AreEqual("result", result);
        }
    }
}
