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

    [Parameter("Where to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")] readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable `OCTOVERSION_CurrentBranch`.", Name = "OCTOVERSION_CurrentBranch")]
    readonly string BranchName;

    [OctoVersion(UpdateBuildNumber = true, BranchMember = nameof(BranchName), AutoDetectBranchMember = nameof(AutoDetectBranch), Framework = "net9.0")]
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
                TeamCity.Instance.SetConfigurationParameter("GitVersion.BranchName", BranchName);
                TeamCity.Instance.SetConfigurationParameter("GitVersion.FullSemVer", FullSemVer);
                TeamCity.Instance.SetConfigurationParameter("GitVersion.NuGetVersion", NuGetVersion);
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
