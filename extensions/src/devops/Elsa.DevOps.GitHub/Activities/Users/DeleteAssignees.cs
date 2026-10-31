using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Users;

/// <summary>
/// Removes assignees from an issue or pull request.
/// </summary>
[Activity(
    "Elsa.GitHub.Users",
    "GitHub Users",
    "Removes assignees from an issue or pull request.",
    DisplayName = "Delete Assignees")]
[UsedImplicitly]
public class DeleteAssignees : GitHubActivity
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
    /// The usernames of the assignees to remove.
    /// </summary>
    [Input(Description = "The usernames of the assignees to remove.")]
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

        // First get the issue to get current assignees
        var issue = await client.Issue.Get(owner, repository, number);
        
        // Create a new assignee list by removing specified assignees
        var currentAssignees = issue.Assignees.Select(a => a.Login).ToList();
        var newAssignees = currentAssignees.Except(assignees).ToList();
        
        // Update the issue with the new set of assignees
        var issueUpdate = new IssueUpdate();
        foreach (var assignee in newAssignees)
        {
            issueUpdate.AddAssignee(assignee);
        }
        
        // If the list is empty, we need to explicitly clear assignees
        if (!newAssignees.Any())
        {
            issueUpdate.ClearAssignees();
        }
        
        var updatedIssue = await client.Issue.Update(owner, repository, number, issueUpdate);
        context.Set(UpdatedIssue, updatedIssue);
    }
}