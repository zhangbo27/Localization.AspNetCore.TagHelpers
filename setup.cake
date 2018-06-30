#load "nuget:https://www.myget.org/F/cake-contrib/api/v2?package=Cake.Recipe&prerelease"
#addin "nuget:?package=Cake.Yarn&version=0.3.6"

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
    shouldExecuteGitLink: IsRunningOnWindows() && BuildSystem.IsRunningOnAppVeyor // We need this so it doesn't fail on appveyor linux builds
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
    testCoverageFilter: "+[Localization.*]* -[*.Tests]*",
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

Build.RunDotNetCore();