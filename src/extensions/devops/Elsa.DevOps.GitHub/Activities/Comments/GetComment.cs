using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Comments;

/// <summary>
/// Retrieves a specific comment from a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Comments",
    "GitHub Comments",
    "Retrieves a specific comment from a GitHub repository.",
    DisplayName = "Get Comment")]
[UsedImplicitly]
public class GetComment : GitHubActivity
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
        var id = context.Get(Id);

        var client = GetClient(context);
        var comment = await client.Issue.Comment.Get(owner, repository, id);

        context.Set(RetrievedComment, comment);
    }
}