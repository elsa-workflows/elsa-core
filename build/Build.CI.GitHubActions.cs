using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;
using Nuke.Components;

[CustomGitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        OnPullRequestBranches = ["main"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        //InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test), nameof(IPack.Pack)],
        InvokedTargets = [nameof(ICompile.Compile), nameof(IPack.Pack)],
        CacheKeyFiles = [],
        ConcurrencyCancelInProgress = true
    )
]
public partial class Build;

class CustomGitHubActionsAttribute : GitHubActionsAttribute
{
    public CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);

        var newSteps = new List<GitHubActionsStep>(job.Steps);

        // only need to list the ones that are missing from default image
        newSteps.Insert(0, new GitHubActionsSetupDotNetStep(["9.x"]));

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsSetupDotNetStep : GitHubActionsStep
{
    public GitHubActionsSetupDotNetStep(string[] versions)
    {
        Versions = versions;
    }

    string[] Versions { get; }

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