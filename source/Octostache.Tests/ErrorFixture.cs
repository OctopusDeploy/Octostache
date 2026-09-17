using FluentAssertions;
using Octostache.Templates;
using Xunit;

namespace Octostache.Tests
{
    public class ErrorFixture
    {
        [Fact]
        public void SimpleError() => Error.Format("message").Should().Be("[SimpleError error: message]");
        
        [Fact]
        public void WithContext() => Error.Format("message", "context").Should().Be("[WithContext context error: message]");
        
        [Fact]
        public void WithCallerOverride() => Error.Format("message", caller: "CallerOverride").Should().Be("[CallerOverride error: message]");
    }
}