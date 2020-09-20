using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class SemverFixture : BaseFixture
    {
        const string Version = "1.2.3.4-branch+meta";
        
        [Fact]
        public void TestSemverMajor()
        {
            var result = Evaluate(
                                  "#{Version | SemverMajor}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("1");
        }
        
        [Fact]
        public void TestSemverMinor()
        {
            var result = Evaluate(
                                  "#{Version | SemverMinor}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("2");
        }
        
        [Fact]
        public void TestSemverPatch()
        {
            var result = Evaluate(
                                  "#{Version | SemverPatch}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("3");
        }
        
        [Fact]
        public void TestSemverRevision()
        {
            var result = Evaluate(
                                  "#{Version | SemverRevision}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("4");
        }
        
        [Fact]
        public void TestSemverRelease()
        {
            var result = Evaluate(
                                  "#{Version | SemverRelease}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("branch");
        }
        
        [Fact]
        public void TestSemverMetadata()
        {
            var result = Evaluate(
                                  "#{Version | SemverMetadata}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("meta");
        }
    }
}