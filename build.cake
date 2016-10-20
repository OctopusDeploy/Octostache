//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine"
#addin "MagicChunks"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var globalAssemblyFile = "./source/Octostache/Properties/AssemblyInfo.cs";
var projectToPackage = "./source/Octostache";

var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

var nugetVersion = gitVersionInfo.NuGetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building Octostache v{0}", nugetVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Default")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateAssemblyVersionInformation")
    .IsDependentOn("__Build")
    .IsDependentOn("__Test")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .IsDependentOn("__Pack")
    .IsDependentOn("__Publish");

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());

Task("__UpdateAssemblyVersionInformation")
    .WithCriteria(isContinuousIntegrationBuild)
    .Does(() =>
{
     GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true,
        UpdateAssemblyInfoFilePath = globalAssemblyFile
    });

    Information("AssemblyVersion -> {0}", gitVersionInfo.AssemblySemVer);
    Information("AssemblyFileVersion -> {0}", $"{gitVersionInfo.MajorMinorPatch}.0");
    Information("AssemblyInformationalVersion -> {0}", gitVersionInfo.InformationalVersion);
    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);
    if(BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);
});

Task("__Build")
    .Does(() =>
{
    DotNetCoreBuild("**/project.json", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__Test")
    .Does(() =>
{
    GetFiles("**/*Tests/project.json")
        .ToList()
        .ForEach(testProjectFile => 
        {
            DotNetCoreTest(testProjectFile.ToString(), new DotNetCoreTestSettings
            {
                Configuration = configuration
            });
        });
});

Task("__UpdateProjectJsonVersion")
    .WithCriteria(isContinuousIntegrationBuild)
    .Does(() =>
{
    var projectToPackagePackageJson = $"{projectToPackage}/project.json";
    Information("Updating {0} version -> {1}", projectToPackagePackageJson, nugetVersion);

    TransformConfig(projectToPackagePackageJson, projectToPackagePackageJson, new TransformationCollection {
        { "version", nugetVersion }
    });
});

Task("__Pack")
    .Does(() =>
{
    DotNetCorePack(projectToPackage, new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDir,
        NoBuild = true
    });
});

Task("__Publish")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    NuGetPush("artifacts/Octostache." + nugetVersion + ".nupkg", new NuGetPushSettings {
        Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
        ApiKey = EnvironmentVariable("MyGetApiKey")
    });
    NuGetPush("artifacts/Octostache." + nugetVersion + ".symbols.nupkg", new NuGetPushSettings {
        Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
        ApiKey = EnvironmentVariable("MyGetApiKey")
    });

    if (gitVersionInfo.PreReleaseTag == "")
    {
        NuGetPush("artifacts/Octostache." + nugetVersion + ".nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });
        NuGetPush("artifacts/Octostache." + nugetVersion + ".symbols.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__Default");
	
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
