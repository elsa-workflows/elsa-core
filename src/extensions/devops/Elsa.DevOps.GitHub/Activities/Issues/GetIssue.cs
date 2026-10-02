using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Issues;

/// <summary>
/// Retrieves details of an existing issue from a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Issues",
    "GitHub Issues",
    "Retrieves details of an existing issue from a GitHub repository.",
    DisplayName = "Get Issue")]
[UsedImplicitly]
public class GetIssue : GitHubActivity
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
    /// The issue number.
    /// </summary>
    [Input(Description = "The issue number.")]
    public Input<int> Number { get; set; } = null!;

    /// <summary>
    /// The retrieved issue.
    /// </summary>
    [Output(Description = "The retrieved issue.")]
    public Output<Issue> RetrievedIssue { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);

        var client = GetClient(context);
        var issue = await client.Issue.Get(owner, repository, number);

        context.Set(RetrievedIssue, issue);
    }
}