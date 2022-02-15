using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class VersionFixture : BaseFixture
    {
        [Theory]
        [InlineData("1", "1", "0", "0", "0", "", "", "", "")]
        [InlineData("1.2", "1", "2", "0", "0", "", "", "", "")]
        [InlineData("1.2.3", "1", "2", "3", "0", "", "", "", "")]
        [InlineData("1.2.3.4", "1", "2", "3", "4", "", "", "", "")]
        [InlineData("1.2.3.4-branch.1", "1", "2", "3", "4", "branch.1", "branch", "1", "")]
        [InlineData("1.2.3.4-branch.1+meta", "1", "2", "3", "4", "branch.1", "branch", "1", "meta")]
        [InlineData("v1.2.3.4-branch.1+meta", "1", "2", "3", "4", "branch.1", "branch", "1", "meta")]
        [InlineData("V1.2.3.4-branch.1+meta", "1", "2", "3", "4", "branch.1", "branch", "1", "meta")]
        [InlineData("V1.2.3.4-branch.hithere+meta", "1", "2", "3", "4", "branch.hithere", "branch", "hithere", "meta")]
        [InlineData("V1.2.3.4-branch-hithere+meta", "1", "2", "3", "4", "branch-hithere", "branch", "hithere", "meta")]
        [InlineData("V1.2.3.4-branch_hithere+meta", "1", "2", "3", "4", "branch_hithere", "branch", "hithere", "meta")]
        [InlineData("19.0.0.Final", "19", "0", "0", "0", "Final", "Final", "", "")]
        [InlineData("284.0.0-debian_component_based", "284", "0", "0", "0", "debian_component_based", "debian", "component_based", "")]
        [InlineData("latest", "0", "0", "0", "0", "latest", "latest", "", "")]
        [InlineData("v1", "1", "0", "0", "0", "", "", "", "")]
        [InlineData("v1.2.3", "1", "2", "3", "0", "", "", "", "")]
        public void TestVersionMajor(string version, string major, string minor, string patch, string revision, string release, string releasePrefix, string releaseCounter, string metadata)
        {
            var variables =  new Dictionary<string, string>
            {
                {"Version", version}
            };

            var majorResult = Evaluate("#{Version | VersionMajor}", variables);
            var minorResult = Evaluate("#{Version | VersionMinor}", variables);
            var patchResult = Evaluate("#{Version | VersionPatch}", variables);
            var revisionResult = Evaluate("#{Version | VersionRevision}", variables);
            var releaseResult = Evaluate("#{Version | VersionPreRelease}", variables);
            var releasePrefixResult = Evaluate("#{Version | VersionPreReleasePrefix}", variables);
            var releaseCounterResult = Evaluate("#{Version | VersionPreReleaseCounter}", variables);
            var metadataResult = Evaluate("#{Version | VersionMetadata}", variables);

            majorResult.Should().Be(major);
            minorResult.Should().Be(minor);
            patchResult.Should().Be(patch);
            revisionResult.Should().Be(revision);
            releaseResult.Should().Be(release);
            releasePrefixResult.Should().Be(releasePrefix);
            releaseCounterResult.Should().Be(releaseCounter);
            metadataResult.Should().Be(metadata);
        }
    }
}