#tool "nuget:?package=GitVersion.CommandLine&version=5.9.0"
#addin "nuget:?package=Cake.Git&version=2.0.0"

#load "utils/pretty-output.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var _target = Argument("target", "Pack");
var _configuration = Argument("configuration", "Debug");
var _buildNumber = Argument("build-number", 1);
var _nugetFeed = Argument("nuget-feed", (string)null);
var _nugetUserName = Argument("nuget-user-name", (string)null);
var _nugetApiKey = Argument("nuget-api-key", (string)null);
var _gitHubPat = Argument("github-pat", (string)null);

///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////

var _rootDir = Directory("..");
var _srcDir = _rootDir + Directory("src");
var _testsDir = _rootDir + Directory("tests");
var _artifactsDir = _rootDir + Directory("artifacts");
var _solutionFile = GetFiles($"{_rootDir}/*.sln").SingleOrDefault() ??
                    throw new InvalidOperationException("Did not find the solution file");

Func<DotNetMSBuildSettings> _getDefaultMSBuildSettings =
    () => new DotNetMSBuildSettings
    {
        NoLogo = true,
        Verbosity = DotNetVerbosity.Minimal
    };

var _isAzurePipelinesBuild = BuildSystem.IsRunningOnAzurePipelines;

var _gitVersionInfo = GitVersion(new GitVersionSettings
{
    Verbosity = GitVersionVerbosity.Diagnostic
});

var _currentBranch = _gitVersionInfo.BranchName;

var _isCI = !BuildSystem.IsLocalBuild;
var _isMainBuild = _currentBranch
    .Equals("main", StringComparison.OrdinalIgnoreCase);

var _version = GetVersion(_gitVersionInfo, _buildNumber, _isMainBuild);
var _buildName = GetBuildName(_version, _buildNumber, _isMainBuild);

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(_ =>
{
    Info($"Branch: {_currentBranch}");
    Info($"Version: {_version}");
    Info($"Configuration: {_configuration}");
    Info($"Is CI? {_isCI}");
    Info($"Is main build? {_isMainBuild}");

    if (_isAzurePipelinesBuild)
    {
        AzurePipelines.Commands.UpdateBuildNumber(_buildName);
    }
});

Teardown(_ =>
{
    if (_isAzurePipelinesBuild)
    {
        var testResultFiles = GetFiles($"{_testsDir}/**/*.trx").ToList();

        if (!testResultFiles.Any())
        {
            return;
        }

        AzurePipelines.Commands.PublishTestResults(new AzurePipelinesPublishTestResultsData
        {
            TestRunTitle = _buildName,
            TestRunner = AzurePipelinesTestRunnerType.VSTest,
            Configuration = _configuration,
            TestResultsFiles = testResultFiles
        });
    }
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    Info($"Cleaning {Relative(_solutionFile)}");

    DotNetClean(_solutionFile.FullPath, new DotNetCleanSettings
    {
        Configuration = _configuration,
        MSBuildSettings = _getDefaultMSBuildSettings()
    });
});

Task("Restore")
    .Does(() =>
{
    Info($"Restoring {Relative(_solutionFile)}");

    DotNetRestore(_solutionFile.FullPath, new DotNetRestoreSettings
    {
        MSBuildSettings = _getDefaultMSBuildSettings()
    });
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Info($"Building {Relative(_solutionFile)}");

    DotNetBuild(_solutionFile.FullPath, new DotNetBuildSettings
    {
        Configuration = _configuration,
        NoRestore = true,
        MSBuildSettings = _getDefaultMSBuildSettings()
            .SetVersion(_version)
            .WithProperty("ContinuousIntegrationBuild", _isCI.ToString().ToLower())
            .WithProperty("Copyright", $"Copyright © {DateTime.Now.Year} Kato Stoelen")
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Info($"Running tests in solution {Relative(_solutionFile)}");

    DotNetTest(_solutionFile.FullPath, new DotNetTestSettings
    {
        Configuration = _configuration,
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => args.Append("--nologo"),
        Loggers = _isAzurePipelinesBuild ? new[] { "trx" } : new string[0]
    });
});

Task("Clean-Artifacts")
    .Does(() =>
{
    if (DirectoryExists(_artifactsDir))
    {
        Info($"Cleaning artifacts directory {Relative(_artifactsDir)}");
        CleanDirectory(_artifactsDir);
    }
    else
    {
        Info($"Creating artifacts directory {Relative(_artifactsDir)}");
        CreateDirectory(_artifactsDir);
    }
});

Task("Pack")
    .IsDependentOn("Build")
    .IsDependentOn("Clean-Artifacts")
    .Does(() =>
{
    var releaseNotes = "(none)";

    Info($"Packing project(s) in solution {Relative(_solutionFile)}");

    DotNetPack(_solutionFile.FullPath, new DotNetPackSettings
    {
        Configuration = _configuration,
        OutputDirectory = _artifactsDir,
        NoRestore = true,
        NoBuild = true,
        MSBuildSettings = _getDefaultMSBuildSettings()
            .SetVersion(_version)
            .WithProperty("Copyright", $"Copyright © {DateTime.Now.Year} Kato Stoelen")
            .WithProperty("PackageReleaseNotes", releaseNotes)
    });
});

Task("Push")
    .IsDependentOn("Pack")
    .WithCriteria(!string.IsNullOrEmpty(_nugetFeed), "NuGet feed not specified")
    .Does(() =>
{
    Info($"Pushing package(s) in {Relative(_artifactsDir)} to {_nugetFeed}");

    var tempConfig = new TemporaryNuGetConfig(
        _nugetFeed,
        _nugetUserName,
        _nugetApiKey,
        Context);

    using (tempConfig)
    {
        NuGetPush(GetFiles($"{_artifactsDir}/*.nupkg"), new NuGetPushSettings
        {
            Source = _nugetFeed,
            ApiKey = _nugetApiKey,
            ConfigFile = tempConfig.Path
        });
    }
});

Task("CI")
    .IsDependentOn("Test")
    .IsDependentOn("Push");

RunTarget(_target);

///////////////////////////////////////////////////////////////////////////////
// CUSTOM
///////////////////////////////////////////////////////////////////////////////

private string GetVersion(
    GitVersion gitVersionInfo, int buildNumber, bool isMainBuild)
{
    if (isMainBuild)
    {
        return gitVersionInfo.MajorMinorPatch;
    }

    if (BuildSystem.IsRunningOnAzurePipelines && AzurePipelines.Environment.PullRequest.IsPullRequest)
    {
        var pullRequestNumber = AzurePipelines.Environment.PullRequest.Number;

        return $"{gitVersionInfo.MajorMinorPatch}-pr{pullRequestNumber}.{buildNumber}";
    }

    return $"{gitVersionInfo.MajorMinorPatch}-{gitVersionInfo.PreReleaseLabel}.{buildNumber}";
}

private string GetBuildName(string version, int buildNumber, bool isMainBuild) =>
    isMainBuild
        ? $"{version} (Build #{buildNumber})"
        : version;

private FilePath Relative(FilePath filePath)
{
    var absoluteRootPath = MakeAbsolute(_rootDir);
    var absoluteFilePath = MakeAbsolute(filePath);

    return absoluteRootPath.GetRelativePath(absoluteFilePath);
}

private DirectoryPath Relative(DirectoryPath directoryPath)
{
    var absoluteRootPath = MakeAbsolute(_rootDir);
    var absoluteDirectoryPath = MakeAbsolute(directoryPath);

    return absoluteRootPath.GetRelativePath(absoluteDirectoryPath);
}

internal class TemporaryNuGetConfig : IDisposable
{
    private readonly string _nugetFeed;
    private readonly string _nugetUserName;
    private readonly string _nugetApiKey;
    private readonly ICakeContext _context;

    public TemporaryNuGetConfig(
        string nugetFeed,
        string nugetUserName,
        string nugetApiKey,
        ICakeContext context)
    {
        _nugetFeed = nugetFeed;
        _nugetUserName = nugetUserName;
        _nugetApiKey = nugetApiKey;
        _context = context;
        Path = context.MakeAbsolute(context.File("nuget.config"));

        CreateConfig();
    }

    public FilePath Path { get; }

    private void CreateConfig()
    {
        _context.Information($"Creating temporary NuGet config file {Path}");

        var exitCode = _context.StartProcess(
            "dotnet",
            new ProcessSettings { WorkingDirectory = _context.Environment.WorkingDirectory, Silent = true }
                .WithArguments(args => args
                    .Append("new")
                    .Append("nugetconfig")));

        if (exitCode != 0)
        {
            throw new Exception($"Failed to create new nuget config (Exit code: {exitCode}");
        }

        _context.NuGetRemoveSource(
            "nuget",
            "https://api.nuget.org/v3/index.json",
            new NuGetSourcesSettings
            {
                ConfigFile = Path
            });

        AddSource();
    }

    private void AddSource()
    {
        var sourceName = new Uri(_nugetFeed).Host.Replace("www.", string.Empty);

        _context.Information($"Adding NuGet source {sourceName}");

        _context.NuGetAddSource(sourceName, _nugetFeed, new NuGetSourcesSettings
        {
            UserName = _nugetUserName,
            Password = _nugetApiKey,
            ConfigFile = Path
        });

        _context.Information(string.Empty);
    }

    private void RemoveConfig()
    {
        _context.Information(string.Empty);
        _context.Information($"Removing temporary NuGet config file {Path}");

        _context.DeleteFile(Path);
    }

    public void Dispose() => RemoveConfig();
}