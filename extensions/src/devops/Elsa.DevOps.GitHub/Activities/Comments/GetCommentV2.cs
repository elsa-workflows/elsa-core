using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Comments;

/// <summary>
/// Retrieves a specific comment from a GitHub repository without colliding with the workflow activity ID.
/// </summary>
[Activity(
    "Elsa.GitHub.Comments",
    "GetComment",
    2,
    "Retrieves a specific comment from a GitHub repository.",
    "GitHub Comments",
    DisplayName = "Get Comment")]
[UsedImplicitly]
public class GetCommentV2 : GitHubActivity
{
    public GetCommentV2()
    {
        Version = 2;
    }

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
    /// The provider comment ID.
    /// </summary>
    [Input(Description = "The comment ID.")]
    public Input<int> CommentId { get; set; } = null!;

    /// <summary>
    /// The retrieved comment.
    /// </summary>
    [Output(Description = "The retrieved comment.")]
    public Output<IssueComment> RetrievedComment { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var commentId = context.Get(CommentId);

        var client = GetClient(context);
        var comment = await client.Issue.Comment.Get(owner, repository, commentId);

        context.Set(RetrievedComment, comment);
    }
}
