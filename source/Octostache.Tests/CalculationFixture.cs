using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class CalculationFixture : BaseFixture
    {
        [Theory]
        [MemberData(nameof(ConditionalIsSupportedData))]
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

        public static IEnumerable<object[]> ConditionalIsSupportedData()
        {
            yield return new object[] { "3+2", "5" };
            yield return new object[] { "3-2", "1" };
            yield return new object[] { "3*2", "6" };
            yield return new object[] { "3/2", (3d / 2).ToString(CultureInfo.CurrentCulture) };
            yield return new object[] { "3*2+2*4", "14" };
            yield return new object[] { "3*(2+2)*4", "48" };
            yield return new object[] { "A+2", "7" };
            yield return new object[] { "A+B", "12" };
            yield return new object[] { "C+2", "#{C+2}" };
            yield return new object[] { "{B}-2", "5" };
            yield return new object[] { "2-B", "-5" };
            yield return new object[] { "(B*2)-2", "12" };
            yield return new object[] { "2-(B*2)", "-12" };
            yield return new object[] { "{B}/2", (7d / 2).ToString(CultureInfo.CurrentCulture) };
            yield return new object[] { "2/B", (2d / 7).ToString(CultureInfo.CurrentCulture) };
            yield return new object[] { "0.2*B", (7d * 0.2).ToString(CultureInfo.CurrentCulture) };
            yield return new object[] { "B*0.2", (7d * 0.2).ToString(CultureInfo.CurrentCulture) };

            //erroneous parsing - variables as rhs operands must be surrounded with "{ ... }" to calc correctly.
            yield return new object[] { "B-2", "#{B-2}" };
            yield return new object[] { "B/2", "#{B/2}" };
        }
    }
}
