using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.PullRequests;

/// <summary>
/// Retrieves details of an existing pull request.
/// </summary>
[Activity(
    "Elsa.GitHub.PullRequests",
    "GitHub Pull Requests",
    "Retrieves details of an existing pull request.",
    DisplayName = "Get Pull Request")]
[UsedImplicitly]
public class GetPullRequest : GitHubActivity
{
    /// <summary>
    /// The owner of the repository.
    /// </summary>
    [Input(Description = "The owner of the repository.")]
    public Input<string> Owner { get; set; } = null!;

    /// <summary>
    /// The name of the repository.
    /// </summary>
    [Input(Description = "The name of the repository.")]
    public Input<string> Repository { get; set; } = null!;

    /// <summary>
    /// The pull request number.
    /// </summary>
    [Input(Description = "The pull request number.")]
    public Input<int> Number { get; set; } = null!;

    /// <summary>
    /// The retrieved pull request.
    /// </summary>
    [Output(Description = "The retrieved pull request.")]
    public Output<PullRequest> RetrievedPullRequest { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);

        var client = GetClient(context);
        var pullRequest = await client.PullRequest.Get(owner, repository, number);

        context.Set(RetrievedPullRequest, pullRequest);
    }
}