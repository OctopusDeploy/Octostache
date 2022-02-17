using System;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Nuke.OctoVersion;
using OctoVersion.Core;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Publish);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [NukeOctoVersion] readonly OctoVersionInfo OctoVersion;
    
    [Parameter(Name = "FeedzIoApiKey")] [Secret] readonly string FeedzApiKey;
    [Parameter] [Secret] readonly string NuGetApiKey;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath LocalPackagesDirectory => RootDirectory / ".." / "LocalPackages";
    AbsolutePath SourceDirectory => RootDirectory / "source"; // 
    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
                         .Before(Restore)
                         .Executes(() =>
                                   {
                                       SourceDirectory.GlobDirectories("**/bin", "**/obj", "**/TestResults").ForEach(EnsureCleanDirectory);
                                       EnsureCleanDirectory(OutputDirectory);
                                   });

    Target Restore => _ => _
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
                                                          .SetVersion(OctoVersion.NuGetVersion)
                                                          .EnableNoRestore());
                                     });

    Target Test => _ => _
                        .DependsOn(Compile)
                        .Executes(() =>
                                  {
                                      DotNetTest(settings => settings
                                                     .SetProjectFile(SourceDirectory / "Octostache.Tests" / "Octostache.Tests.csproj")
                                                     .SetConfiguration(Configuration)
                                                     .EnableNoBuild()
                                                     .SetLoggers("trx"));
                                  });

    Target Pack => _ => _
                        .DependsOn(Test)
                        .Executes(() =>
                                  {
                                      DotNetPack(settings => settings
                                                             .SetProject(Solution)
                                                             .SetConfiguration(Configuration)
                                                             .SetOutputDirectory(ArtifactsDirectory)
                                                             .EnableNoBuild()
                                                             .SetVersion(OctoVersion.NuGetVersion));
                                  });

    Target CopyToLocalPackages => _ => _
                                       .DependsOn(Pack)
                                       .OnlyWhenStatic(() => IsLocalBuild)
                                       .Executes(() =>
                                                 {
                                                     EnsureCleanDirectory(LocalPackagesDirectory);
                                                     CopyFileToDirectory(ArtifactsDirectory / $"Octostache.{OctoVersion.NuGetVersion}.nupkg", LocalPackagesDirectory);
                                                 });

    Target Publish => _ => _
                           .DependsOn(Pack)
                           .OnlyWhenStatic(() => !IsLocalBuild)
                           .Executes(() =>
                                     {
                                         NuGetTasks.NuGetPush(settings => settings
                                                                          .SetTargetPath(ArtifactsDirectory / $"Octostache.{OctoVersion.NuGetVersion}.nupkg")
                                                                          .SetSource("https://f.feedz.io/octopus-deploy/dependencies/nuget")
                                                                          .SetApiKey(FeedzApiKey)
                                                                          .SetProcessArgumentConfigurator(args => args.Add("-SkipDuplicate")));

                                         if (OctoVersion.PreReleaseTagWithDash == "")
                                         {
                                             NuGetTasks.NuGetPush(settings => settings
                                                                              .SetTargetPath(ArtifactsDirectory / $"Octostache.{OctoVersion.NuGetVersion}.nupkg")
                                                                              .SetSource("https://www.nuget.org/api/v2/package")
                                                                              .SetApiKey(NuGetApiKey)
                                                                              .SetProcessArgumentConfigurator(args => args.Add("-SkipDuplicate")));
                                         }
                                     });

    Target Default => _ => _
                           .DependsOn(Publish)
                           .DependsOn(CopyToLocalPackages);
}