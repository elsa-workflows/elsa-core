using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Users;

/// <summary>
/// Adds assignees to an issue or pull request.
/// </summary>
[Activity(
    "Elsa.GitHub.Users",
    "GitHub Users",
    "Adds assignees to an issue or pull request.",
    DisplayName = "Add Assignees")]
[UsedImplicitly]
public class AddAssignees : GitHubActivity
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
    /// The issue or pull request number.
    /// </summary>
    [Input(Description = "The issue or pull request number.")]
    public Input<int> Number { get; set; } = null!;

    /// <summary>
    /// The usernames of the assignees to add.
    /// </summary>
    [Input(Description = "The usernames of the assignees to add.")]
    public Input<IEnumerable<string>> Assignees { get; set; } = null!;

    /// <summary>
    /// The issue with updated assignees.
    /// </summary>
    [Output(Description = "The issue with updated assignees.")]
    public Output<Issue> UpdatedIssue { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);
        var assignees = context.Get(Assignees)!;

        var client = GetClient(context);
        
        var issueUpdate = new IssueUpdate();
        foreach (var assignee in assignees)
        {
            issueUpdate.AddAssignee(assignee);
        }

        var issue = await client.Issue.Update(owner, repository, number, issueUpdate);
        context.Set(UpdatedIssue, issue);
    }
}