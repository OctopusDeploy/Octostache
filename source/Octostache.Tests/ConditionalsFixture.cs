﻿using System;
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

        [Fact]
        public void ConditionalIsSupportedWithLeadingWhitespace()
        {
            var result = Evaluate("#{ if Truthy}#{Result}#{/if}",
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
        public void ConditionalIsSupportedWithTrailingWhitespace()
        {
            var result = Evaluate("#{if Truthy }#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "Truthy", "true" },
                });

            result.Should().Be("result");

            result = Evaluate("#{if Truthy  }#{Result}#{/if}",
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
        public void ConditionalToStringIsSupported()
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
        public void ConditionalNegationIsSupported()
        {
            var result = Evaluate("#{if Octopus != \"software\"}#{Result}#{/if}",
                new Dictionary<string, string>
                {
                    { "Result", "result" },
                    { "software", "something else" },
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
    }
}
