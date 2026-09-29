using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Issues;

/// <summary>
/// Creates a new issue in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Issues",
    "GitHub Issues",
    "Creates a new issue in a GitHub repository.",
    DisplayName = "Create Issue")]
[UsedImplicitly]
public class CreateIssue : GitHubActivity
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
    /// The title of the issue.
    /// </summary>
    [Input(Description = "The title of the issue.")]
    public Input<string> Title { get; set; } = null!;

    /// <summary>
    /// The body content of the issue.
    /// </summary>
    [Input(Description = "The body content of the issue.")]
    public Input<string?> Body { get; set; } = null!;

    /// <summary>
    /// The list of labels to apply to the issue.
    /// </summary>
    [Input(Description = "The list of labels to apply to the issue.")]
    public Input<IEnumerable<string>?> Labels { get; set; } = null!;

    /// <summary>
    /// The milestone to associate with the issue.
    /// </summary>
    [Input(Description = "The milestone to associate with the issue.")]
    public Input<int?> MilestoneId { get; set; } = null!;

    /// <summary>
    /// The usernames of the assignees to add to the issue.
    /// </summary>
    [Input(Description = "The usernames of the assignees to add to the issue.")]
    public Input<IEnumerable<string>?> Assignees { get; set; } = null!;

    /// <summary>
    /// The created issue.
    /// </summary>
    [Output(Description = "The created issue.")]
    public Output<Issue> CreatedIssue { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var title = context.Get(Title)!;
        var body = context.Get(Body);
        var labels = context.Get(Labels);
        var milestoneId = context.Get(MilestoneId);
        var assignees = context.Get(Assignees);

        var client = GetClient(context);

        var newIssue = new NewIssue(title)
        {
            Body = body
        };

        if (labels != null)
        {
            foreach (var label in labels)
            {
                newIssue.Labels.Add(label);
            }
        }

        if (milestoneId.HasValue)
        {
            newIssue.Milestone = milestoneId.Value;
        }

        if (assignees != null)
        {
            foreach (var assignee in assignees)
            {
                newIssue.Assignees.Add(assignee);
            }
        }

        var issue = await client.Issue.Create(owner, repository, newIssue);
        context.Set(CreatedIssue, issue);
    }
}