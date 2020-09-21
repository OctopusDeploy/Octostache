using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class MavenFixture : BaseFixture
    {
        const string Version = "1.2.3-4branch";
        
        [Fact]
        public void TestMavenMajor()
        {
            var result = Evaluate(
                                  "#{Version | MavenMajor}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("1");
        }
        
        [Fact]
        public void TestMavenMinor()
        {
            var result = Evaluate(
                                  "#{Version | MavenMinor}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("2");
        }
        
        [Fact]
        public void TestMavenPatch()
        {
            var result = Evaluate(
                                  "#{Version | MavenPatch}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("3");
        }
        
        [Fact]
        public void TestMavenRevision()
        {
            var result = Evaluate(
                                  "#{Version | MavenRevision}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("4");
        }
        
        [Fact]
        public void TestMavenRelease()
        {
            var result = Evaluate(
                                  "#{Version | MavenRelease}", 
                                  new Dictionary<string, string>
                                  {
                                      {"Version", Version}
                                  });

            result.Should().Be("branch");
        }
    }
}