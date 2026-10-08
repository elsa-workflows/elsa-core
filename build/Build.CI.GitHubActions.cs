using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;
using Nuke.Components;

[CustomGitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        OnPullRequestBranches = ["main", "patch/*", "develop/*", "release/*", "codex/elsa-integration-program"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test)],
        CacheKeyFiles = [],
        ConcurrencyCancelInProgress = true
    )
]
public partial class Build;

class CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : GitHubActionsAttribute(name, image, images)
{
    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);

        var newSteps = new List<GitHubActionsStep>(job.Steps);

        // only need to list the ones that are missing from default image
        newSteps.Insert(0, new GitHubActionsSetupDotNetStep(["10.x"]));
        newSteps.Add(new GitHubActionsUploadTestResultsStep());

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsUploadTestResultsStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: Upload test results and hang dumps");
        using (writer.Indent())
        {
            writer.WriteLine("if: failure()");
            writer.WriteLine("uses: actions/upload-artifact@v4");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("name: test-results-and-hang-dumps");
                writer.WriteLine("if-no-files-found: ignore");
                writer.WriteLine("path: |");
                using (writer.Indent())
                {
                    writer.WriteLine("testresults/");
                    writer.WriteLine("**/*.dmp");
                    writer.WriteLine("**/*Sequence*.xml");
                }
            }
        }
    }
}

class GitHubActionsSetupDotNetStep(string[] versions) : GitHubActionsStep
{
    string[] Versions { get; } = versions;

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/setup-dotnet@v4");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("dotnet-version: |");
                using (writer.Indent())
                {
                    foreach (var version in Versions)
                    {
                        writer.WriteLine(version);
                    }
                }
            }
        }
    }
}