using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class CalculationFixture : BaseFixture
    {
        [Theory]
        [InlineData("3+2", "5")]
        [InlineData("3-2", "1")]
        [InlineData("3*2", "6")]
        [InlineData("3/2", "1,5")]
        [InlineData("3*2+2*4", "14")]
        [InlineData("3*(2+2)*4", "48")]
        [InlineData("A+2", "7")]
        [InlineData("A+B", "12")]
        [InlineData("C+2", "#{C+2}")]
        public void ConditionalIsSupported(string expression, string expectedResult)
        {
            var result = Evaluate($"#{{calc {expression}}}",
                new Dictionary<string, string>
                {
                    { "A", "5" },
                    { "B", "7" },
                });

            result.Should().Be(expectedResult);
        }
    }
}
