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
        OnPullRequestExcludePaths = ["docs/**/*", "package.json", "package-lock.json", "readme.md"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test), nameof(IPack.Pack)],
        CacheKeyFiles = []
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
        newSteps.Insert(0, new GitHubActionsUseGnuTarStep());

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsUseGnuTarStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- if: ${{ runner.os == 'Windows' }}");
        using (writer.Indent())
        {
            writer.WriteLine("name: 'Use GNU tar'");
            writer.WriteLine("shell: cmd");
            writer.WriteLine("run: |");
            using (writer.Indent())
            {
                writer.WriteLine("echo \"Adding GNU tar to PATH\"");
                writer.WriteLine("echo C:\\Program Files\\Git\\usr\\bin>>\"%GITHUB_PATH%\"");
            }
        }
    }
}