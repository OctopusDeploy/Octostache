//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#tool "dotnet:?package=GitVersion.Tool&version=5.3.6"
#tool "nuget:?package=OctopusTools&version=9.0.0"
#addin nuget:?package=Cake.Git&version=1.1.0

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
    var currentBranch = GitBranchCurrent(DirectoryPath.FromString(".")).FriendlyName;
    var octopusServer = EnvironmentVariable("OctopusServerUrl");
    var octopusApiKey = EnvironmentVariable("OctopusServerApiKey");
    var space = EnvironmentVariable("OctopusServerSpaceName");
    var octopusProjectName = "Octostache";

    var nugetPackage = GetFiles($"{artifactsDir}Octostache.{nugetVersion}.nupkg");

    // Current config for this repo doesn't generate prerelease tags, even if we're on a development/feature branch.
    // Using the --overwrite-mode=IgnoreIfExists flag instructs the target Octopus server to ignore any attempted uploads with the same verison number.
    // This decision is made server-side, so you'll still see log messages indicating the package was uploaded. Never fear, the push is discarded if the version already exists. 
    // You can verify this by looking at the SHA1 and Published date of the package in the package feed: it won't change on subsequent pushes.
    OctoPush(octopusServer, octopusApiKey, nugetPackage, new OctopusPushSettings 
    {
        ArgumentCustomization = args => args.Append("--overwrite-mode=IgnoreIfExists"),
        Space = space 
    });

    // Config-as-Code doesn't yet support Automatic Release Creation, so do it manually
    OctoCreateRelease(octopusProjectName, new CreateReleaseSettings {
        ArgumentCustomization = args => args.Append($"--gitRef={currentBranch}"),
        Server = octopusServer,
        ApiKey = octopusApiKey,
        ReleaseNumber = nugetVersion,
        Space = space,
        Packages = new Dictionary<string, string>
        {
            { "Octostache", nugetVersion }
        },
        IgnoreExisting = true
     });
});

Task("Default")
    .IsDependentOn("Publish")
    .IsDependentOn("CopyToLocalPackages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
