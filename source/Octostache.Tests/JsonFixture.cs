using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [TestCase("{\"Hello\": \"World\"}", "#{Test[Hello]}", ExpectedResult = "World", TestName = "Simple Indexing")]
        [TestCase("{\"Hello\": \"World\"}", "#{Test.Hello}", ExpectedResult = "World", TestName = "Simple Dot Notation")
        ]
        [TestCase("{\"Hello\": {\"World\": {\"Foo\": {\"Bar\": 12 }}}}", "#{Test[Hello][World][Foo][Bar]}",
             ExpectedResult = "12", TestName = "Deep")]
        [TestCase("{\"Items\": [{\"Name\": \"Toast\"}, {\"Name\": \"Bread\"}]}", "#{Test.Items[1].Name}",
             ExpectedResult = "Bread", TestName = "Arrays")]
        [TestCase("{\"Foo\": {\"Bar\":\"11\"}}", "#{Test.Foo}", ExpectedResult = "{\"Bar\":\"11\"}",
             TestName = "Raw JSON returned")]
        public string JsonParsing(string json, string pattern)
        {
            var variables = new VariableDictionary
            {
                ["Test"] = json
            };

            return variables.Evaluate(pattern);
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
        public void JsonWithSelfRefferentialVariableFails()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "{Name: \"#{Test.Value}\", Value: 12}",
            };


            var ex = Assert.Throws<InvalidOperationException>(() => variables.Evaluate("#{Test.Name}"));
            Assert.That(ex.Message, Does.Contain("appears to have resulted in a self referencing loop"));
        }

    }
}
