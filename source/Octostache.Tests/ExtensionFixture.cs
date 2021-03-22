using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class ExtensionFixture : BaseFixture
    {
        [Fact]
        public void TestCustomBuiltInExtension()
        {
            var variables = new VariableDictionary();
            variables.Add("Foo", "Bar");

            variables.AddExtension("testFunc", ExtensionFixture.ToLower);

            var result = variables.Evaluate("#{Foo|testFunc}");

            result.Should().Be("bar");
        }

        public static string ToLower(string argument, string[] options)
        {
            return options.Any() ? null : argument?.ToLower();
        }


        [Fact]
        public void TestCustomExtension()
        {
            var variables = new VariableDictionary();
            variables.Add("Foo", "Bar");

            variables.AddExtension("supercoolfunc", ExtensionFixture.SuperCoolFunc);

            var result = variables.Evaluate("#{Foo|supercoolfunc}");

            result.Should().Be("2-1-18");
        }

        public static string SuperCoolFunc(string argument, string[] options)
        {
            return string.Join("-", argument.Select(c => (((int)char.ToUpper(c)) - 64).ToString()));
        }
    }
}