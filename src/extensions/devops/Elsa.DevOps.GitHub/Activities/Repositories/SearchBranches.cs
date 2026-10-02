using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Repositories;

/// <summary>
/// Lists branches in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Repositories",
    "GitHub Repositories",
    "Lists branches in a GitHub repository.",
    DisplayName = "Search Branches")]
[UsedImplicitly]
public class SearchBranches : GitHubActivity
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
    /// Optional filter for branch name.
    /// </summary>
    [Input(Description = "Optional filter for branch name (case-insensitive partial match).")]
    public Input<string?> NameFilter { get; set; } = null!;

    /// <summary>
    /// List of branches.
    /// </summary>
    [Output(Description = "List of branches.")]
    public Output<IReadOnlyList<Branch>> Branches { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var nameFilter = context.Get(NameFilter);

        var client = GetClient(context);
        var branches = await client.Repository.Branch.GetAll(owner, repository);

        // Apply filter if specified
        if (!string.IsNullOrEmpty(nameFilter))
        {
            branches = branches
                .Where(b => b.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        context.Set(Branches, branches);
    }
}