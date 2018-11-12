using System;
using Xunit;
using FluentAssertions;
using YamlDotNet.Serialization;

namespace Octostache.Tests
{

    public class YamlFixture : BaseFixture
    {
        [Fact]
        public void YamlDoesNotOverrideExisting()
        {
            var yaml = @"foo: Go Away
product:
    - sku         : BL394D
      quantity    : 4
      description : Basketball
      price       : 450.00";
            
            var variables = new VariableDictionary
            {
                ["Test.Hello"] = "Go Away",
                ["Test"] = yaml,
                ["Test[Foo]"] = "Nope",
                ["Test.Donkey.Kong"] = "MARIO",
            };

            variables.Evaluate("#{Test.Hello}").Should().Be("Go Away");
            variables.Evaluate("#{Test[foo]}").Should().Be("Nope");
            variables.Evaluate("#{Test.Donkey.Kong}").Should().Be("MARIO");
        }
        
        
        [Fact]
        public void YamlSupportsVariableInVariable()
        {
            var variables = new VariableDictionary
            {
                ["Prop"] = "Foo",
                ["Val"] = "Bar",
                ["Test"] = "#{Prop}: #{Val}",
            };

            variables.Evaluate("#{Test[Foo]}").Should().Be("Bar");
            variables.Evaluate("#{Test.Foo}").Should().Be("Bar");
            variables.Evaluate("#{Test[#{Prop}]}").Should().Be("Bar");
        }
        
        
        [Theory]
        [InlineData("Hello: World", "#{Test[Hello]}", "World", "Simple Indexing")]
        [InlineData("Hello: World", "#{Test.Hello}", "World", "Simple Dot Notation")]
        [InlineData(@"Hello:
    World:
        Foo:
            Bar: 12", "#{Test[Hello][World][Foo][Bar]}", "12", "Deep")]
        [InlineData(@"Items:
  - Name: Toast
  - Name: Bread", "#{Test.Items[1].Name}", "Bread", "Arrays")]
        [InlineData(@"Foo:
                        Bar: 11", "#{Test.Foo}", "Bar: 11", "Raw YAML returned")]
        [InlineData(@"Name: '#{Test.Value}'
Desc: Monkey
Value: 12", "#{Test.Name}", "12", "Non-Direct inner YAML")]
        public void SuccessfulYamlParsing(string json, string pattern, string expectedResult, string testName)
        { 
            var variables = new VariableDictionary
            {
                ["Test"] = json
            };

            variables.Evaluate(pattern).Should().Be(expectedResult);
        }

        
        
        [Fact]
        public void Foo()
        {

            var d = new Deserializer().Deserialize<object>(@"AAPL:
  - shares: -75.088
    date: 11/27/2015
  - shares: 75.088
    date: 11/26/2015");
        }
    }
    
    
    public class JsonFixture :BaseFixture
    {
        [Fact]
        public void JsonDoesNotOverrideExisting()
        {
            var variables = new VariableDictionary
            {
                ["Test.Hello"] = "Go Away",
                ["Test"] = "{\"Hello\": \"World\", \"Foo\": \"Bar\", \"Donkey\" : {\"Kong\": 12}}",
                ["Test[Foo]"] = "Nope",
                ["Test.Donkey.Kong"] = "MARIO",
            };

            variables.Evaluate("#{Test.Hello}").Should().Be("Go Away");
            variables.Evaluate("#{Test[Foo]}").Should().Be("Nope");
            variables.Evaluate("#{Test.Donkey.Kong}").Should().Be("MARIO");
        }

        [Fact]
        public void JsonSupportsVariableInVariable()
        {
            var variables = new VariableDictionary
            {
                ["Prop"] = "Foo",
                ["Val"] = "Bar",
                ["Test"] = "{#{Prop}: \"#{Val}\"}",
            };

            variables.Evaluate("#{Test[Foo]}").Should().Be("Bar");
            variables.Evaluate("#{Test.Foo}").Should().Be("Bar");
            variables.Evaluate("#{Test[#{Prop}]}").Should().Be("Bar");
        }

        [Theory]
        [InlineData("{\"Hello\": \"World\"}", "#{Test[Hello]}", "World", "Simple Indexing")]
        [InlineData("{\"Hello\": \"World\"}", "#{Test.Hello}", "World", "Simple Dot Notation")]
        [InlineData("{\"Hello\": {\"World\": {\"Foo\": {\"Bar\": 12 }}}}", "#{Test[Hello][World][Foo][Bar]}", "12", "Deep")]
        [InlineData("{\"Items\": [{\"Name\": \"Toast\"}, {\"Name\": \"Bread\"}]}", "#{Test.Items[1].Name}", "Bread", "Arrays")]
        [InlineData("{\"Foo\": {\"Bar\":\"11\"}}", "#{Test.Foo}", "{\"Bar\":\"11\"}", "Raw JSON returned")]
        [InlineData("{Name: \"#{Test.Value}\", Desc: \"Monkey\", Value: 12}", "#{Test.Name}", "12", "Non-Direct inner JSON")]
        public void SuccessfulJsonParsing(string json, string pattern, string expectedResult, string testName)
        { 
            var variables = new VariableDictionary
            {
                ["Test"] = json
            };

            variables.Evaluate(pattern).Should().Be(expectedResult);
        }

        [Fact]
        public void JsonInvalidDoesNotReplace()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "{Name: NoQuote}",
            };

            variables.Evaluate("#{Test.Name}").Should().Be("#{Test.Name}");
        }

        [Fact]
        public void JsonArraySupportsIterator()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "[2,3,5,8]",
            };

            var pattern = "#{each number in Test}#{number}#{if Octopus.Template.Each.Last == \"False\"}-#{/if}#{/each}";

            variables.Evaluate(pattern).Should().Be("2-3-5-8");
        }

        [Fact]
        public void JsonArraySafeguardedFromNullValues()
        {
            var variables = new VariableDictionary
            {
                ["Test"] = "{Blah: null}",
            };

            var pattern = "Before:#{each number in Test.Blah}#{number}#{/each}:After";

            variables.Evaluate(pattern).Should().Be("Before::After");
        }

        [Fact]
        public void JsonObjectSupportsIterator()
        {
            var variables = new VariableDictionary
            {
                ["Octopus.Sizes"] = "{\"Small\": \"11.5\",  Large: 15.21}",
            };

            var pattern = @"#{each size in Octopus.Sizes}#{size}:#{size.Value},#{/each}";

            variables.Evaluate(pattern).Should().Be("Small:11.5,Large:15.21,");
        }


        [Fact]
        public void JsonEvaluatesConditionalsWithEscapes()
        {
            var variables = new VariableDictionary
            {
                ["Foo"] = "test text"
            };

            var pattern = "{\"Bar\":\"#{if Foo == \\\"test text\\\"}Blaa#{/if}\"}";

            variables.Evaluate(pattern).Should().Be("{\"Bar\":\"Blaa\"}");
        }


        [Fact]
        public void JsonObjectSupportsIteratorWithInnerSelection()
        {
            var variables = new VariableDictionary
            {
                ["Octopus.Sizes"] = "{\"X-Large\": {\"Error\": \"Not Stocked\"}}",
            };

            var pattern = @"#{each size in Octopus.Sizes}#{size.Key} - #{size.Value.Error}#{/each}";

            variables.Evaluate(pattern).Should().Be("X-Large - Not Stocked");
        }

        [Fact]
        public void NullJsonPropertyTreatedAsEmptyString()
        {
            var variables = new VariableDictionary
            {
                ["Foo"] = "{Bar: null}",
            };

            var pattern = @"Alpha#{Foo.Bar}bet";

            variables.Evaluate(pattern).Should().Be("Alphabet");
        }
    }
}
