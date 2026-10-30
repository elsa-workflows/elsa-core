using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Repositories;

/// <summary>
/// Retrieves details of an existing repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Repositories",
    "GitHub Repositories",
    "Retrieves details of an existing repository.",
    DisplayName = "Get Repository")]
[UsedImplicitly]
public class GetRepository : GitHubActivity
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
    /// The retrieved repository.
    /// </summary>
    [Output(Description = "The retrieved repository.")]
    public Output<Repository> RetrievedRepository { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;

        var client = GetClient(context);
        var repo = await client.Repository.Get(owner, repository);

        context.Set(RetrievedRepository, repo);
    }
}