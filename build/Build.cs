using System;
using System.IO;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")]
    readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter(
        "Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable OCTOVERSION_CurrentBranch.",
        Name = "OCTOVERSION_CurrentBranch")]
    readonly string BranchName;

    [OctoVersion(UpdateBuildNumber = true, BranchParameter = nameof(BranchName),
        AutoDetectBranchParameter = nameof(AutoDetectBranch), Framework = "net6.0")]
    readonly OctoVersionInfo OctoVersionInfo;
    
    AbsolutePath SourceDirectory => RootDirectory / "source";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath LocalPackagesDirectory => RootDirectory / ".." / "LocalPackages";
    AbsolutePath OctostacheFolder => SourceDirectory / "Octostache";

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory
                .GlobDirectories("**/bin", "**/obj", "**/TestResults")
                .ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(OctoVersionInfo.FullSemVer)
                .SetFileVersion(OctoVersionInfo.FullSemVer)
                .SetInformationalVersion(OctoVersionInfo.InformationalVersion)
                .EnableNoRestore());
        });
    
    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(_ => _
                .SetProject(Solution.Octostache)
                .SetVersion(OctoVersionInfo.FullSemVer)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .DisableIncludeSymbols()
                .SetVerbosity(DotNetVerbosity.Normal));
            // .SetProcessArgumentConfigurator(args =>
            // {
            //     args.Add("/p:NuspecFile=" + Solution.Octostache.Directory / "Octostache.nuspec");
            //     return args;
            // })
            // .SetProperty("NuspecFile", Solution.Octostache.Directory / "Octostache.nuspec")
            // .SetProperty("NuspecProperties", $"Version={OctoVersionInfo.FullSemVer}"));
            //
            // var nuspec = "Octostache.nuspec";
            // var nuspecPath = OctostacheFolder / nuspec;
            // try
            // {
            //     ReplaceTextInFiles(nuspecPath, "<version>$version$</version>", $"<version>{OctoVersionInfo.FullSemVer}</version>");
            //     ReplaceTextInFiles(nuspecPath, "\\$configuration$\\", $"\\{Configuration.ToString()}\\");
            //
            //     DotNetPack(_ => _
            //         .SetProject(Solution)
            //         .SetProcessArgumentConfigurator(args =>
            //         {
            //             args.Add("/p:NuspecFile=" + nuspec);
            //             return args;
            //         })
            //         .SetVersion(OctoVersionInfo.FullSemVer)
            //         .SetConfiguration(Configuration)
            //         .SetOutputDirectory(ArtifactsDirectory)
            //         .EnableNoBuild());
            // }
            // finally
            // {
            //     ReplaceTextInFiles(nuspecPath, $"<version>{OctoVersionInfo.FullSemVer}</version>", "<version>$version$</version>");
            //     ReplaceTextInFiles(nuspecPath, $"\\{Configuration.ToString()}\\", "\\$configuration$\\");
            // }
        });
    
    [PublicAPI]
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(Solution.Octostache)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore());
        });
    
    [UsedImplicitly]
    Target CopyToLocalPackages => _ => _
        .OnlyWhenStatic(() => IsLocalBuild)
        .TriggeredBy(Pack)
        .Executes(() =>
        {
            EnsureExistingDirectory(LocalPackagesDirectory);
            CopyFileToDirectory(ArtifactsDirectory / $"{Solution.Name}.{OctoVersionInfo.FullSemVer}.nupkg", LocalPackagesDirectory, FileExistsPolicy.Overwrite);
        });
    
    void ReplaceTextInFiles(AbsolutePath path, string oldValue, string newValue)
    {
        var fileText = File.ReadAllText(path);
        fileText = fileText.Replace(oldValue, newValue);
        File.WriteAllText(path, fileText);
    }

    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Pack);
}
