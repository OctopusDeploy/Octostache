using System;
using NUnit.Framework;

namespace Octostache.Tests
{
    [TestFixture]
    public class JsonFixture :BaseFixture
    {
        [Test]
        public void JsonDoesNotOverrideExisting()
        {
            var variables = new VariableDictionary
            {
                ["Test.Hello"] = "Go Away",
                ["Test"] = "{\"Hello\": \"World\", \"Foo\": \"Bar\", \"Donkey\" : {\"Kong\": 12}}",
                ["Test[Foo]"] = "Nope",
                ["Test.Donkey.Kong"] = "MARIO",
            };

            Assert.AreEqual("Go Away", variables.Evaluate("#{Test.Hello}"));
            Assert.AreEqual("Nope", variables.Evaluate("#{Test[Foo]}"));
            Assert.AreEqual("MARIO", variables.Evaluate("#{Test.Donkey.Kong}"));
        }

        [Test]
        public void JsonSupportsVariableInVariable()
        {
            var variables = new VariableDictionary
            {
                ["Prop"] = "Foo",
                ["Val"] = "Bar",
                ["Test"] = "{#{Prop}: \"#{Val}\"}",
            };

            Assert.AreEqual("Bar", variables.Evaluate("#{Test[Foo]}"));
            Assert.AreEqual("Bar", variables.Evaluate("#{Test.Foo}"));
            Assert.AreEqual("Bar", variables.Evaluate("#{Test[#{Prop}]}"));
        }

        [Test]
        [TestCase("{\"Hello\": \"World\"}", "#{Test[Hello]}", "World", TestName = "Simple Indexing")]
        [TestCase("{\"Hello\": \"World\"}", "#{Test.Hello}", "World", TestName = "Simple Dot Notation")]
        [TestCase("{\"Hello\": {\"World\": {\"Foo\": {\"Bar\": 12 }}}}", "#{Test[Hello][World][Foo][Bar]}", "12", TestName = "Deep")]
        [TestCase("{\"Items\": [{\"Name\": \"Toast\"}, {\"Name\": \"Bread\"}]}", "#{Test.Items[1].Name}", "Bread", TestName = "Arrays")]
        [TestCase("{\"Foo\": {\"Bar\":\"11\"}}", "#{Test.Foo}", "{\"Bar\":\"11\"}", TestName = "Raw JSON returned")]
        [TestCase("{Name: \"#{Test.Value}\", Desc: \"Monkey\", Value: 12}", "#{Test.Name}", "12", TestName = "Non-Direct inner JSON", Description = "Non -Direct inner JSON reference can resolve if quoted.")]
        public void SuccessfulJsonParsing(string json, string pattern, string expectedResult)
        {
            var variables = new VariableDictionary
            {
                ["Test"] = json
            };

            Assert.That(variables.Evaluate(pattern), Is.EqualTo(expectedResult));
        }

        [Test]
        public void JsonInvalidDoesNotReplace()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "{Name: NoComma}",
            };

            Assert.AreEqual("#{Test.Name}", variables.Evaluate("#{Test.Name}"));
        }

        [Test]
        public void JsonArraySupportsIterator()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "[2,3,5,8]",
            };

            var pattern = "#{each number in Test}#{number}#{if Octopus.Template.Each.Last == \"False\"}-#{/if}#{/each}";

            Assert.AreEqual("2-3-5-8", variables.Evaluate(pattern));
        }

        [Test]
        public void JsonObjectSupportsIterator()
        {
            var variables = new VariableDictionary
            {
                ["Octopus.Sizes"] = "{\"Small\": \"11.5\",  Large: 15.21}",
            };

            var pattern = @"#{each size in Octopus.Sizes}#{size}:#{size.Value},#{/each}";

            Assert.AreEqual("Small:11.5,Large:15.21,", variables.Evaluate(pattern));
        }

        [Test]
        public void JsonObjectSupportsIteratorWithInnerSelection()
        {
            var variables = new VariableDictionary
            {
                ["Octopus.Sizes"] = "{\"X-Large\": {\"Error\": \"Not Stocked\"}}",
            };

            var pattern = @"#{each size in Octopus.Sizes}#{size.Key} - #{size.Value.Error}#{/each}";

            Assert.AreEqual("X-Large - Not Stocked", variables.Evaluate(pattern));
        }
    }
}
