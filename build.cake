//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#tool "dotnet:?package=GitVersion.Tool&version=5.3.6"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var localPackagesDir = "../LocalPackages";

GitVersion gitVersionInfo;
string nugetVersion;


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Building Octostache v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
    CleanDirectories("./source/**/TestResults");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetCoreRestore("source");
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild("./source", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest("./source/Octostache.Tests/Octostache.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        ArgumentCustomization = args => args.Append("-l trx")
    });
});

Task("Pack")
    .IsDependentOn("Test")
    .Does(() =>
{
    DotNetCorePack("./source/Octostache", new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDir,
        NoBuild = true,
        ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
    });

    DeleteFiles(artifactsDir + "*symbols*");
});

Task("CopyToLocalPackages")
    .IsDependentOn("Pack")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .Does(() =>
{
    CreateDirectory(localPackagesDir);
    CopyFileToDirectory($"{artifactsDir}/Octostache.{nugetVersion}.nupkg", localPackagesDir);
});

Task("Publish")
    .IsDependentOn("Pack")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    NuGetPush($"{artifactsDir}Octostache.{nugetVersion}.nupkg", new NuGetPushSettings {
        Source = "https://f.feedz.io/octopus-deploy/dependencies/nuget",
        ApiKey = EnvironmentVariable("FeedzIoApiKey"),
        SkipDuplicate = true,
    });

    if (gitVersionInfo.PreReleaseTagWithDash == "")
    {
        NuGetPush($"{artifactsDir}Octostache.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey"),
            SkipDuplicate = true,
        });
    }
});

Task("Default")
    .IsDependentOn("Publish")
    .IsDependentOn("CopyToLocalPackages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
