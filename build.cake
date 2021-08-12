#addin "Cake.FileHelpers"
#addin "nuget:?package=Cake.Coveralls"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=coveralls.io"

var target = Argument<string>("target", "Default");

var	isTagged = (
	BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
	!string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name)
);

var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("Configuration") :
    "Release";
var preReleaseSuffix =
    HasArgument("PreReleaseSuffix") ? Argument<string>("PreReleaseSuffix") :
    isTagged ? null :
    EnvironmentVariable("PreReleaseSuffix") != null ? EnvironmentVariable("PreReleaseSuffix") :
    "beta";
var buildNumber =
    HasArgument("BuildNumber") ? Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number :
    TravisCI.IsRunningOnTravisCI ? TravisCI.Environment.Build.BuildNumber :
    EnvironmentVariable("BuildNumber") != null ? int.Parse(EnvironmentVariable("BuildNumber")) :
    0;

var isLocalBuild = BuildSystem.IsLocalBuild;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;

var nugetApiKey =
    HasArgument("nugetApiKey") ? Argument<string>("nugetApiKey") : EnvironmentVariable("NUGET_API_KEY");
var githubApiKey = EnvironmentVariable("GITHUB_API_KEY");
var coverallsApiKey = EnvironmentVariable("COVERALLS_API_KEY");

var testCoverageFilter = "+[Pomodoro]* -[Pomodoro.Console]*";
var testCoverageExcludeByAttribute = "*.ExcludeFromCodeCoverage*";
var testCoverageExcludeByFile = "*/*Designer.cs;*/*AssemblyInfo.cs";
var restoreSources = new [] {
     "https://www.myget.org/F/xunit/api/v3/index.json",
     "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
     "https://dotnet.myget.org/F/cli-deps/api/v3/index.json",
     "https://api.nuget.org/v3/index.json"
};
var githubOwner = "jrgcubano";
var githubRepo = "Pomodoro";
var githubRawUri = "http://raw.githubusercontent.com";

var outputDir = Directory("./build");
var nugetDir = outputDir + Directory("nuget");
var testResultsDir = outputDir + Directory("test-results");
var coverageFilePath = testResultsDir + File("coverage.xml");

Task("Clean")
	.Does(() => 
    {        
        CleanDirectories(outputDir);
        DeleteDirectories(GetDirectories("**/bin"), true);
        DeleteDirectories(GetDirectories("**/obj"), true);

        if(!DirectoryExists(outputDir))
            CreateDirectory(outputDir);
        if(!DirectoryExists(testResultsDir))
            CreateDirectory(testResultsDir);        
        if(!DirectoryExists(nugetDir))
            CreateDirectory(nugetDir);    
	});


Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => 
    {        
        var settings = new DotNetCoreRestoreSettings {
            Sources = restoreSources
        };
        var srcProjects = GetFiles("./src/**/*.csproj");
        var testProjects = GetFiles("./test/**/*.csproj");
        foreach(var project in srcProjects)
        {
            DotNetCoreRestore(project.GetDirectory().FullPath, settings);
        }    
        foreach(var project in testProjects)
        {
            DotNetCoreRestore(project.GetDirectory().FullPath, settings);
        }    
    });

 Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    { 
        var srcProjects = GetFiles("./src/**/*.csproj");
        var testProjects = GetFiles("./test/**/*.csproj");
        foreach(var project in srcProjects)
        {
            DotNetCoreBuild(
                project.GetDirectory().FullPath,
                new DotNetCoreBuildSettings()
                {                
                    Configuration = configuration
                });
        }           
        foreach(var project in testProjects)
        {
            DotNetCoreBuild(
                project.GetDirectory().FullPath,
                new DotNetCoreBuildSettings()
                {          
                    Configuration = configuration
                });
        }           
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {        
        var projects = GetFiles("./test/**/*.csproj");
        foreach(var project in projects)
        {
            // var testXmlPath = testResultsDir.Path.CombineWithFilePath(project.GetFilenameWithoutExtension()).FullPath + ".trx";                    
            var testXmlPathAbs = MakeAbsolute(testResultsDir)
                .CombineWithFilePath(
                    project.GetFilenameWithoutExtension()).FullPath + ".trx";
            Information("File Abs: " + testXmlPathAbs);
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    // ArgumentCustomization = args => args.Append("-xml").Append(testXmlPath),
                    // ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + testXmlPathAbs + "\""),
                    Configuration = configuration,
                    NoBuild = true                    
                });
        } 
    });

Task("Test-Coverage")
	.IsDependentOn("Build")
	.Does(() =>
{    
    var projects = GetFiles("./test/**/*.csproj");
    foreach(var project in projects)
    {
        var testXmlPathAbs = MakeAbsolute(testResultsDir)
                .CombineWithFilePath(
                    project.GetFilenameWithoutExtension()).FullPath + ".trx";    
        Action<ICakeContext> testAction = tool => tool.DotNetCoreTest(
            project.ToString(), new DotNetCoreTestSettings {
                NoBuild = true,
                Verbose = false,
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--logger \"trx;LogFileName=" + testXmlPathAbs + "\"")
            }
        );    
        OpenCover(testAction,
            coverageFilePath,
            new OpenCoverSettings {
                ReturnTargetCodeOffset = 0,
                ArgumentCustomization = args => args.Append("-mergeoutput -oldStyle")
            }
            .WithFilter(testCoverageFilter)
            .ExcludeByAttribute(testCoverageExcludeByAttribute)
            .ExcludeByFile(testCoverageExcludeByFile));
    }
});

Task("Upload-Coverage-Report")
    .WithCriteria(() => FileExists(coverageFilePath))
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .IsDependentOn("Test-Coverage")
    .Does(() =>
{
    CoverallsIo(coverageFilePath, new CoverallsIoSettings()
    {
        RepoToken = coverallsApiKey
    });
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() => 
    {
        string versionSuffix = null;
        if (!string.IsNullOrEmpty(preReleaseSuffix))
        {
            versionSuffix = preReleaseSuffix + "-" + buildNumber.ToString("D4");
        }
        var srcProjects = GetFiles("./src/Pomodoro/*.csproj");
        foreach (var project in srcProjects)
        {
            DotNetCorePack(
                project.GetDirectory().FullPath,
                new DotNetCorePackSettings()
                {
                    Configuration = configuration,
                    OutputDirectory = nugetDir,
                    VersionSuffix = versionSuffix,
                    NoBuild = true,
                    Verbose = false		            
                });
        }
    });

Task("Publish-Package")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isTagged)
    .IsDependentOn("Package")
    .Does(() =>
{
    var packages = GetFiles(nugetDir.Path.FullPath + "/*.nupkg");
    NuGetPush(packages, new NuGetPushSettings {
        Source = "https://www.nuget.org/api/v2/package",
        ApiKey = nugetApiKey 
    });
});

Task("DefaultCI")
    //.IsDependentOn("Test-Coverage")
    //.IsDependentOn("Upload-Coverage-Report")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Publish-Package");

Task("Default") 
    .IsDependentOn("Package");

RunTarget(target);