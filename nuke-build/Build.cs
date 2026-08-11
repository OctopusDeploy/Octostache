using System;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    static readonly string Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")] readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable `OCTOVERSION_CurrentBranch`.", Name = "OCTOVERSION_CurrentBranch")]
    readonly string BranchName;

    [Parameter("Patch number override for OctoVersion")] readonly int? PatchNumberOverride;

    [OctoVersion(UpdateBuildNumber = true, BranchMember = nameof(BranchName), AutoDetectBranchMember = nameof(AutoDetectBranch), PatchMember = nameof(PatchNumberOverride),
        Framework = "net10.0")]
    readonly OctoVersionInfo OctoVersionInfo;

    static AbsolutePath SourceDirectory => RootDirectory / "source";
    static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    static AbsolutePath LocalPackagesDirectory => RootDirectory / ".." / "LocalPackages";

    string FullSemVer => IsLocalBuild
        ? $"{OctoVersionInfo.FullSemVer}-{Timestamp}"
        : OctoVersionInfo.FullSemVer;

    string NuGetVersion => IsLocalBuild
        ? $"{OctoVersionInfo.NuGetVersion}-{Timestamp}"
        : OctoVersionInfo.NuGetVersion;

    [UsedImplicitly]
    Target CalculateVersion => _ => _
        .OnlyWhenStatic(() => TeamCity.Instance != null)
        .Executes(() =>
            {
                // Provides backwards compatibility with expected GitVersion configuration

                // TeamCity supplies the pull request parameters as the literal "N/A" on builds that
                // aren't from a pull request, which is enough for IsPullRequest to report true. Check
                // the source branch is usable as well, otherwise master builds end up with a branch
                // name of "N/A" and anything consuming it, such as Octopus create-release --gitRef,
                // fails to resolve a git reference.
                var pullRequestSourceBranch = TeamCity.Instance.PullRequestSourceBranch;
                var isPullRequest = TeamCity.Instance.IsPullRequest
                    && !string.IsNullOrWhiteSpace(pullRequestSourceBranch)
                    && !pullRequestSourceBranch.Equals("N/A", StringComparison.OrdinalIgnoreCase);

                // Use the actual branch name for PR builds rather than pull/xxx
                TeamCity.Instance.SetConfigurationParameter("GitVersion.BranchName", isPullRequest ? pullRequestSourceBranch : BranchName);

                TeamCity.Instance.SetConfigurationParameter("GitVersion.FullSemVer", FullSemVer);
                TeamCity.Instance.SetConfigurationParameter("GitVersion.NuGetVersion", NuGetVersion);

                // Make sure the patch version may be propagated to the build action
                if (OctoVersionInfo.Patch.HasValue)
                {
                    TeamCity.Instance.SetConfigurationParameter("GitVersion.PatchNumber", OctoVersionInfo.Patch.Value.ToString());
                }
            }
        );

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
                ArtifactsDirectory.CreateOrCleanDirectory();
            }
        );

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
            {
                DotNetTasks.DotNetRestore(s => s.SetProjectFile(Solution));
            }
        );

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
            {
                DotNetTasks.DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetVersion(FullSemVer)
                    .SetInformationalVersion(OctoVersionInfo.InformationalVersion)
                    .EnableNoRestore()
                );
            }
        );

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
            {
                DotNetTasks.DotNetTest(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetLoggers("trx")
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .EnableBlameCrash()
                );
            }
        );

    Target Pack => _ => _
        .DependsOn(Test)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
            {
                DotNetTasks.DotNetPack(s => s
                    .SetProject(Solution.Octostache)
                    .SetVersion(FullSemVer)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoBuild()
                );
            }
        );

    [UsedImplicitly]
    Target CopyToLocalPackages => _ => _
        .OnlyWhenStatic(() => IsLocalBuild)
        .DependsOn(Pack)
        .Executes(() =>
            {
                LocalPackagesDirectory.CreateDirectory();
                ArtifactsDirectory.GlobFiles("*.nupkg")
                    .ForEach(x => x.CopyToDirectory(LocalPackagesDirectory, ExistsPolicy.FileOverwrite));
            }
        );

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}
