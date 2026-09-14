using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;
using Nuke.Components;

[CustomGitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        OnPullRequestBranches = ["main", "patch/*", "develop/*"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile)],
        CacheKeyFiles = [],
        ConcurrencyCancelInProgress = true,
        ReadPermissions = [GitHubActionsPermissions.Contents],
        WritePermissions = [GitHubActionsPermissions.Checks, GitHubActionsPermissions.PullRequests]
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
        newSteps.Add(new GitHubActionsRunTestsStep());
        newSteps.Add(new GitHubActionsUploadTestResultsStep());
        newSteps.Add(new GitHubActionsPublishTestResultsStep());

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsRunTestsStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Run: TUnit tests with coverage'");
        using (writer.Indent())
        {
            writer.WriteLine("run: |");
            using (writer.Indent())
            {
                writer.WriteLine("dotnet test --solution Elsa.sln \\");
                writer.WriteLine("  --configuration Release \\");
                writer.WriteLine("  --no-build \\");
                writer.WriteLine("  --results-directory artifacts/test-results/pr \\");
                writer.WriteLine("  -- \\");
                writer.WriteLine("  --coverage \\");
                writer.WriteLine("  --coverage-settings \"$GITHUB_WORKSPACE/test/coverage.settings.xml\" \\");
                writer.WriteLine("  --coverage-output-format cobertura \\");
                writer.WriteLine("  --report-trx");
            }
        }
    }
}

class GitHubActionsUploadTestResultsStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Upload: TUnit test results and coverage'");
        using (writer.Indent())
        {
            writer.WriteLine("if: ${{ !cancelled() }}");
            writer.WriteLine("uses: actions/upload-artifact@v4");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("name: pr-test-results");
                writer.WriteLine("path: artifacts/test-results/pr");
                writer.WriteLine("if-no-files-found: warn");
            }
        }
    }
}

class GitHubActionsPublishTestResultsStep : GitHubActionsStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Publish: TUnit test results'");
        using (writer.Indent())
        {
            writer.WriteLine("if: ${{ !cancelled() && github.event.pull_request.head.repo.full_name == github.repository && github.event.pull_request.user.login != 'dependabot[bot]' }}");
            writer.WriteLine("uses: EnricoMi/publish-unit-test-result-action@v2");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("files: artifacts/test-results/pr/**/*.trx");
                writer.WriteLine("check_name: TUnit Test Results");
                writer.WriteLine("comment_title: TUnit Test Results");
                writer.WriteLine("comment_mode: always");
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
