//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#tool "dotnet:?package=GitVersion.Tool&version=5.3.6"
#tool "nuget:?package=TeamCity.Dotnet.Integration&version=1.0.10"

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

var teamCityMsBuildLoggerGlob = "./tools/TeamCity.Dotnet.Integration**/build/**/msbuild15/TeamCity.MSBuild.Logger.dll";
var teamCityMsBuildLoggerPath = GetFiles(teamCityMsBuildLoggerGlob).SingleOrDefault();
if (teamCityMsBuildLoggerPath == null)
    throw new Exception($"The TeamCityMSBuildLogger was expected to be in the path '{teamCityMsBuildLoggerGlob}' but wasn't available.");
else Information($"Using {teamCityMsBuildLoggerPath}");

var teamCityVsTestAdapterGlob = "./tools/TeamCity.Dotnet.Integration**/build/**/vstest15/TeamCity.VSTest.TestAdapter.dll";
var teamCityVsTestAdapterPath = GetFiles(teamCityVsTestAdapterGlob).SingleOrDefault();
if (teamCityVsTestAdapterPath == null)
    throw new Exception($"The TeamCityVSTestAdapter was expected to be in the path '{teamCityVsTestAdapterGlob}' but wasn't available.");
else Information($"Using {teamCityVsTestAdapterPath}");

private DotNetCoreMSBuildSettings DotNetCoreMsBuildSettings()
{
	var settings = new DotNetCoreMSBuildSettings {
		MaxCpuCount = 32
	};

	if (BuildSystem.IsRunningOnTeamCity)
	{
		return settings
			.DisableConsoleLogger()
			.WithLogger(teamCityMsBuildLoggerPath.FullPath, "TeamCity.MSBuild.Logger.TeamCityMSBuildLogger", "teamcity");
	}

	return settings;
}

private DotNetCoreVSTestSettings DotNetCoreVSTestSettings()
{
	var settings = new DotNetCoreVSTestSettings
	{
		Logger = "console;verbosity=normal",
		Parallel = true
	};

	if (BuildSystem.IsRunningOnTeamCity)
	{
		settings.Logger = "teamcity";
		settings.TestAdapterPath = teamCityVsTestAdapterPath.FullPath;
	}

	return settings;
}

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
        ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}"),
        MSBuildSettings = DotNetCoreMsBuildSettings()
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreVSTest("./source/Octostache.Tests/Octostache.Tests.csproj", DotNetCoreVSTestSettings());
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
