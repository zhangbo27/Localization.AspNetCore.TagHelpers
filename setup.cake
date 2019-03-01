#load "nuget:https://ci.appveyor.com/nuget/cake-recipe-pylg5x5ru9c2?package=Cake.Recipe&prerelease&version=0.3.0-alpha0464"
#addin "nuget:?package=Cake.Coverlet&version=2.2.1"
#addin "nuget:?package=Cake.Yarn&version=0.4.2"

Environment.SetVariableNames();

BuildParameters.SetParameters(
    context: Context,
    buildSystem: BuildSystem,
    sourceDirectoryPath: "./src",
    testDirectoryPath: "./test",
    title: "Localization.AspNetCore.TagHelpers",
    repositoryOwner: "WormieCorp",
    repositoryName: "Localization.AspNetCore.TagHelpers",
    appVeyorAccountName: "AdmiringWorm",
    solutionFilePath: "./Localization.AspNetCore.TagHelpers.sln",
    shouldRunInspectCode: false,
    shouldRunDotNetCorePack: true,
    shouldRunCodecov: true,
    shouldExecuteGitLink: false // We disable gitlink as it doesn't work for .NET Core anyhow
);

ToolSettings.SetToolSettings(
    context: Context,
    dupFinderExcludePattern: new string[] {
        BuildParameters.RootDirectoryPath + "/test/**/*.cs",
        BuildParameters.SourceDirectoryPath + "/Localization.Demo"
    },
    dupFinderExcludeFilesByStartingCommentSubstring: new string[] {
        "<auto-generated>"
    },
    testCoverageFilter: "+[Localization*]* -[*.Tests]*",
    testCoverageExcludeByAttribute: "*.ExcludeFromCodeCoverage*",
    testCoverageExcludeByFile: "*Designer.cs;*7*.g.cs;*.g.i.cs;*.g.cs"
);

Task("Client-Packages")
  .Does(() => {

  Yarn.FromPath("./src/Localization.Demo").Install();
})
  .OnError(exception =>
  {
    Warning("Unable to restore Client packages, but continuing with the rest of the tasks");
  });

BuildParameters.Tasks.DotNetCoreBuildTask.IsDependentOn("Client-Packages");

((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Actions.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Criterias.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Dependencies.Clear();

BuildParameters.Tasks.DotNetCoreTestTask
    .IsDependentOn("Install-ReportGenerator")
    .Does(() => {
    var projects = GetFiles(BuildParameters.TestDirectoryPath + (BuildParameters.TestFilePattern ?? "/**/*Tests.csproj"));
    var testFileName = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetFilename();
    var testDirectory = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetDirectory();

    var settings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover,
        CoverletOutputDirectory = testDirectory,
        CoverletOutputName = testFileName.ToString(),
        MergeWithFile = BuildParameters.Paths.Files.TestCoverageOutputFilePath
    };
    foreach (var line in ToolSettings.TestCoverageExcludeByFile.Split(';')) {
        foreach (var file in GetFiles("**/" + line)) {
            settings.WithFileExclusion(file.FullPath);
        }
    }

    foreach (var attr in ToolSettings.TestCoverageExcludeByAttribute.Split(';'))
    {
      settings.WithAttributeExclusion(attr);
    }

    foreach(var filter in ToolSettings.TestCoverageFilter.Split(' '))
    {
      if (filter[0] == '+') {
        settings.WithInclusion(filter.TrimStart('+'));
      } else
      {
        settings.WithFilter(filter.TrimStart('-'));
      }
    }

    var testSettings = new DotNetCoreTestSettings {
        Configuration = BuildParameters.Configuration,
        NoBuild = true
    };

    foreach (var project in projects) {
        DotNetCoreTest(project.FullPath, testSettings, settings);
    }

    if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath)) {
        ReportGenerator(BuildParameters.Paths.Files.TestCoverageOutputFilePath, BuildParameters.Paths.Directories.TestCoverage);
    }
});

Task("AppVeyor-Linux")
  .IsDependentOn("Upload-AppVeyor-Artifacts");

Build.RunDotNetCore();
