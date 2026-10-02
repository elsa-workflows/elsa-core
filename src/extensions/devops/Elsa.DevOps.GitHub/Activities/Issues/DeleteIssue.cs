using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Issues;

/// <summary>
/// Removes an existing issue by closing it (GitHub doesn't allow actual deletion of issues).
/// </summary>
[Activity(
    "Elsa.GitHub.Issues",
    "GitHub Issues",
    "Removes an existing issue by closing it (GitHub doesn't allow actual deletion of issues).",
    DisplayName = "Delete Issue")]
[UsedImplicitly]
public class DeleteIssue : GitHubActivity
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
    /// The closed issue.
    /// </summary>
    [Output(Description = "The closed issue.")]
    public Output<Issue> ClosedIssue { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);

        var client = GetClient(context);

        // GitHub doesn't allow actual deletion of issues, so we close it instead
        var issueUpdate = new IssueUpdate
        {
            State = ItemState.Closed
        };

        var issue = await client.Issue.Update(owner, repository, number, issueUpdate);
        context.Set(ClosedIssue, issue);
    }
}