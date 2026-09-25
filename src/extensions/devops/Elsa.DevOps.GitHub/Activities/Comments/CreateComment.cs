using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Comments;

/// <summary>
/// Creates a new comment on an issue or pull request in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Comments",
    "GitHub Comments",
    "Creates a new comment on an issue or pull request in a GitHub repository.",
    DisplayName = "Create Comment")]
[UsedImplicitly]
public class CreateComment : GitHubActivity
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
    /// The comment body text.
    /// </summary>
    [Input(Description = "The comment body text.")]
    public Input<string> Body { get; set; } = null!;

    /// <summary>
    /// The created comment.
    /// </summary>
    [Output(Description = "The created comment.")]
    public Output<IssueComment> CreatedComment { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);
        var body = context.Get(Body)!;

        var client = GetClient(context);
        var comment = await client.Issue.Comment.Create(owner, repository, number, body);

        context.Set(CreatedComment, comment);
    }
}