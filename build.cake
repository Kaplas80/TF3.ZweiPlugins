#load "nuget:?package=PleOps.Cake&version=0.7.0"

Task("Define-Project")
    .Description("Fill specific project information")
    .Does<BuildInfo>(info =>
{
	info.CoverageTarget = 0;
    info.PreviewNuGetFeed = "https://nuget.pkg.github.com/Kaplas80/index.json";
    info.PreviewNuGetFeedToken = info.GitHubToken;
    info.StableNuGetFeed = "https://nuget.pkg.github.com/Kaplas80/index.json";
    info.StableNuGetFeedToken = info.GitHubToken;

    info.AddApplicationProjects("TF3.YarhlPlugin.ZweiArges");
    info.AddLibraryProjects("TF3.YarhlPlugin.ZweiArges");
    info.AddTestProjects("TF3.Tests.ZweiArges");
});

Task("Default")
    .IsDependentOn("Stage-Artifacts");

string target = Argument("target", "Default");
RunTarget(target);
