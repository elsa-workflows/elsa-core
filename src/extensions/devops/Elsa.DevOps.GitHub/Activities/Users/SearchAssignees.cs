using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Users;

/// <summary>
/// Lists assignees for issues in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Users",
    "GitHub Users",
    "Lists assignees for issues in a GitHub repository.",
    DisplayName = "Search Assignees")]
[UsedImplicitly]
public class SearchAssignees : GitHubActivity
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
    /// The page number to retrieve.
    /// </summary>
    [Input(Description = "The page number to retrieve.")]
    public Input<int?> Page { get; set; } = null!;

    /// <summary>
    /// The number of records per page.
    /// </summary>
    [Input(Description = "The number of records per page.")]
    public Input<int?> PageSize { get; set; } = null!;

    /// <summary>
    /// List of assignees.
    /// </summary>
    [Output(Description = "List of assignees.")]
    public Output<IReadOnlyList<User>> Assignees { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var page = context.Get(Page);
        var pageSize = context.Get(PageSize);

        var client = GetClient(context);
        
        var options = new ApiOptions();
        if (page.HasValue)
            options.StartPage = page.Value;
            
        if (pageSize.HasValue)
            options.PageSize = pageSize.Value;
            
        var assignees = await client.Issue.Assignee.GetAllForRepository(owner, repository, options);
        context.Set(Assignees, assignees);
    }
}