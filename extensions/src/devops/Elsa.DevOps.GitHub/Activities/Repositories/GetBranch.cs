using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Repositories;

/// <summary>
/// Retrieves details of a specific branch in a repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Repositories",
    "GitHub Repositories",
    "Retrieves details of a specific branch in a repository.",
    DisplayName = "Get Branch")]
[UsedImplicitly]
public class GetBranch : GitHubActivity
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
    /// The name of the branch.
    /// </summary>
    [Input(Description = "The name of the branch.")]
    public Input<string> BranchName { get; set; } = null!;

    /// <summary>
    /// The retrieved branch.
    /// </summary>
    [Output(Description = "The retrieved branch.")]
    public Output<Branch> RetrievedBranch { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var branchName = context.Get(BranchName)!;

        var client = GetClient(context);
        var branch = await client.Repository.Branch.Get(owner, repository, branchName);

        context.Set(RetrievedBranch, branch);
    }
}