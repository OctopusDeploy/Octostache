using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class ConditionalsFixture : BaseFixture
    {
        [Fact]
        public void ConditionalIsSupported()
        {
            var result = Evaluate("#{if Truthy}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "true" },
                });

            result.Should().Be("result");
        }

        [Theory]
        [InlineData("#{ if Truthy}#{Result}#{/if}")]
        [InlineData("#{  if Truthy}#{Result}#{/if}")]
        [InlineData("#{if Truthy }#{Result}#{/if}")]
        [InlineData("#{ if Truthy  }#{Result}#{/if}")]
        [InlineData("#{if  Truthy}#{Result}#{/if}")]
        [InlineData("#{if Truthy}#{ Result}#{/if}")]
        [InlineData("#{if Truthy}#{  Result}#{/if}")]
        [InlineData("#{if Truthy}#{Result }#{/if}")]
        [InlineData("#{if Truthy}#{Result  }#{/if}")]
        [InlineData("#{if Truthy  == \"true\"}#{Result}#{/if}")]
        [InlineData("#{if Truthy  != \"false\"}#{Result}#{/if}")]
        [InlineData("#{if Truthy  |  ToLower  != \"false\"}#{Result}#{/if}")]
        public void ConditionalIgnoresWhitespacesCorrectly(string input)
        {
            var result = Evaluate(input,
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "true" },
                });

            result.Should().Be("result");

            result = Evaluate("#{  if Truthy}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "true" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ConditionalUnlessIsSupported()
        {
            var result = Evaluate("#{unless Truthy}#{Result}#{/unless}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "false" },
                });

            result.Should().Be("result");
        }

        [Theory]
        [InlineData("#{if MyVar}True#{/if}", "")]
        [InlineData("#{unless MyVar}False#{/unless}", "False")]
        public void UnknownVariablesAreTreatedAsFalsy(string template, string expected)
        {
            var result = Evaluate(template, new Dictionary<string, string>());
            result.Should().Be(expected);
        }

        [Fact]
        public void UnknownVariablesOnBothSidesAreTreatedAsEqual()
        {
            var result = Evaluate("#{if Unknown1 == Unknown2}Equal#{/if}", new Dictionary<string, string>());
            result.Should().Be("Equal");
        }

        [Fact]
        public void ConditionalToOtherDictValueIsSupported()
        {
            var result = Evaluate("#{if Octopus == Compare}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Octopus", "octopus" },
                    { "Compare", "octopus" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ConditionalToStringIsSupportedWhenStringIsOnTheRightHandSide()
        {
            var result = Evaluate("#{if Octopus == \"octopus\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Octopus", "octopus" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ConditionalToStringIsSupportedWhenStringIsOnTheLeftHandSide()
        {
            var result = Evaluate("#{if \"octopus\" == Octopus }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Octopus", "octopus" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ConditionalNegationIsSupportedWhenStringIsOnTheLeftHandSide()
        {
            var result = Evaluate("#{if \"software\" != Octopus}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Octopus", "something else" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ConditionalNegationIsSupportedWhenStringIsOnTheRightHandSide()
        {
            var result = Evaluate("#{if Octopus != \"software\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Octopus", "something else" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void NestedConditionalsAreSupported()
        {
            var result = Evaluate("#{if Truthy}#{if Fooey==\"foo\"}#{Result}#{/if}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "true" },
                    { "Fooey", "foo" },
                });

            result.Should().Be("result");
        }

        [Theory]
        [InlineData("true", "result")]
        [InlineData("false", null)]
        public void ConditionalsWithNestedNullShouldReturnCorrect(string truthyValue, string expectedValue)
        {
            var result = Evaluate("#{if Truthy}#{Result}#{else}#{ | null }#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "ElseResult", "elseresult" },
                    { "Truthy", truthyValue },
                });

            result.Should().Be(expectedValue);
        }

        [Fact]
        public void ElseIsSupportedTrue()
        {
            var result = Evaluate("#{if Truthy}#{Result}#{else}#{ElseResult}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "ElseResult", "elseresult" },
                    { "Truthy", "true" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ElseIsSupportedFalse()
        {
            var result = Evaluate("#{if Truthy}#{Result}#{else}#{ElseResult}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "ElseResult", "elseresult" },
                    { "Truthy", "false" },
                });

            result.Should().Be("elseresult");
        }

        [Fact]
        public void NestIfInElse()
        {
            var result = Evaluate("#{if Truthy}#{Result}#{else}#{if Fooey==\"foo\"}#{ElseResult}#{/if}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "ElseResult", "elseresult" },
                    { "Fooey", "foo" },
                    { "Truthy", "false" },
                });

            result.Should().Be("elseresult");
        }

        [Fact]
        public void FunctionCallIsSupported()
        {
            var result = Evaluate("#{if Hello | Contains \"O\" }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Hello", "HELLO" },
                    { "Result", "result" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void FunctionCallIsSupportedWithStringOnTheRightHandSide()
        {
            var result = Evaluate("#{if Hello | ToLower == \"hello\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Hello", "HELLO" },
                    { "Result", "result" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void FunctionCallIsSupportedWithStringOnTheLeftHandSide()
        {
            var result = Evaluate("#{if \"hello\" == Hello | ToLower }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Hello", "HELLO" },
                    { "Result", "result" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void FunctionCallIsSupportedOnBothSide()
        {
            var result = Evaluate("#{if Greeting | ToLower == Hello | ToLower }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Greeting", "Hello" },
                    { "Hello", "HELLO" },
                    { "Result", "result" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void ChainedFunctionCallIsSupported()
        {
            var result = Evaluate("#{if Greeting | Trim | ToUpper | ToLower == Hello | ToBase64 | FromBase64 | Trim | ToLower }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Greeting", " Hello " },
                    { "Hello", "  HELLO " },
                    { "Result", "result" },
                });

            result.Should().Be("result");
        }

        [Fact]
        public void UnknownFunctionsAreEchoed()
        {
            const string template = "#{if Greeting | NonExistingFunction}#{Result}#{/if}";
            var result = Evaluate(template,
                new Dictionary<string, string>
                {
                    { "Greeting", "Hello world" },
                    { "Result", "result" },
                });

            result.Should().Be(template);
        }

        [Fact]
        public void UnknownVariablesAsFunctionArgumentsAreEchoed()
        {
            const string template = "#{if Greeting | TpUpper}#{Result}#{/if}";
            var result = Evaluate(template,
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "MyVar", "Value" },
                });

            result.Should().Be(template);
        }
    }
}
