using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.DevOps.GitHub.Activities.Comments;

/// <summary>
/// Deletes a comment from a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Comments",
    "GitHub Comments",
    "Deletes a comment from a GitHub repository.",
    DisplayName = "Delete Comment")]
[UsedImplicitly]
public class DeleteComment : GitHubActivity
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
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var id = context.Get(Id);

        var client = GetClient(context);
        await client.Issue.Comment.Delete(owner, repository, id);
    }
}