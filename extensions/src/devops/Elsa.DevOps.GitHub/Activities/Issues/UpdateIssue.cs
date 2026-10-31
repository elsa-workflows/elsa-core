using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Issues;

/// <summary>
/// Updates an existing issue in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Issues",
    "GitHub Issues",
    "Updates an existing issue in a GitHub repository.",
    DisplayName = "Update Issue")]
[UsedImplicitly]
public class UpdateIssue : GitHubActivity
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
    /// The updated title of the issue.
    /// </summary>
    [Input(Description = "The updated title of the issue.")]
    public Input<string?> Title { get; set; } = null!;

    /// <summary>
    /// The updated body content of the issue.
    /// </summary>
    [Input(Description = "The updated body content of the issue.")]
    public Input<string?> Body { get; set; } = null!;

    /// <summary>
    /// The state of the issue (open or closed).
    /// </summary>
    [Input(Description = "The state of the issue (open or closed).")]
    public Input<ItemState?> State { get; set; } = null!;

    /// <summary>
    /// The milestone to associate with the issue.
    /// </summary>
    [Input(Description = "The milestone to associate with the issue.")]
    public Input<int?> MilestoneId { get; set; } = null!;

    /// <summary>
    /// The updated issue.
    /// </summary>
    [Output(Description = "The updated issue.")]
    public Output<Issue> UpdatedIssue { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);
        var title = context.Get(Title);
        var body = context.Get(Body);
        var state = context.Get(State);
        var milestoneId = context.Get(MilestoneId);

        var client = GetClient(context);

        var issueUpdate = new IssueUpdate();

        if (title != null)
            issueUpdate.Title = title;

        if (body != null)
            issueUpdate.Body = body;

        if (state.HasValue)
            issueUpdate.State = state.Value;

        if (milestoneId.HasValue)
            issueUpdate.Milestone = milestoneId.Value;

        var issue = await client.Issue.Update(owner, repository, number, issueUpdate);
        context.Set(UpdatedIssue, issue);
    }
}