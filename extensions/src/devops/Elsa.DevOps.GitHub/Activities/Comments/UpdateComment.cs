using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Comments;

/// <summary>
/// Updates an existing comment in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Comments",
    "GitHub Comments",
    "Updates an existing comment in a GitHub repository.",
    DisplayName = "Update Comment")]
[UsedImplicitly]
public class UpdateComment : GitHubActivity
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
    /// The comment ID.
    /// </summary>
    [Input(Description = "The comment ID.")]
    public Input<int> Id { get; set; } = null!;

    /// <summary>
    /// The updated comment body text.
    /// </summary>
    [Input(Description = "The updated comment body text.")]
    public Input<string> Body { get; set; } = null!;

    /// <summary>
    /// The updated comment.
    /// </summary>
    [Output(Description = "The updated comment.")]
    public Output<IssueComment> UpdatedComment { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var id = context.Get(Id);
        var body = context.Get(Body)!;

        var client = GetClient(context);
        var comment = await client.Issue.Comment.Update(owner, repository, id, body);

        context.Set(UpdatedComment, comment);
    }
}