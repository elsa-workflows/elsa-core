using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Milestones;

/// <summary>
/// Searches for milestones in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Milestones",
    "GitHub Milestones",
    "Searches for milestones in a GitHub repository.",
    DisplayName = "Search Milestones")]
[UsedImplicitly]
public class SearchMilestones : GitHubActivity
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
    /// The milestone state to filter by.
    /// </summary>
    [Input(Description = "The milestone state to filter by (open, closed, all).")]
    public Input<string?> State { get; set; } = null!;

    /// <summary>
    /// The sort field to order the milestones.
    /// </summary>
    [Input(Description = "The sort field to order the milestones (due_date, completeness).")]
    public Input<string?> Sort { get; set; } = null!;

    /// <summary>
    /// The direction to sort the milestones.
    /// </summary>
    [Input(Description = "The direction to sort the milestones (asc or desc).")]
    public Input<string?> SortDirection { get; set; } = null!;

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
    /// The retrieved milestones.
    /// </summary>
    [Output(Description = "The retrieved milestones.")]
    public Output<IReadOnlyList<Milestone>> Milestones { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var state = context.Get(State);
        var sort = context.Get(Sort);
        var sortDirection = context.Get(SortDirection);
        var page = context.Get(Page);
        var pageSize = context.Get(PageSize);

        var client = GetClient(context);
        var request = new MilestoneRequest();

        // Set the state if provided
        if (!string.IsNullOrEmpty(state))
        {
            request.State = state.ToLowerInvariant() switch
            {
                "closed" => ItemStateFilter.Closed,
                "all" => ItemStateFilter.All,
                _ => ItemStateFilter.Open
            };
        }

        // Set the sort if provided
        if (!string.IsNullOrEmpty(sort))
        {
            request.SortProperty = sort.ToLowerInvariant() switch
            {
                "completeness" => MilestoneSort.Completeness,
                _ => MilestoneSort.DueDate
            };
        }

        // Set the sort direction if provided
        if (!string.IsNullOrEmpty(sortDirection))
        {
            request.SortDirection = sortDirection.ToLowerInvariant() == "asc"
                ? Octokit.SortDirection.Ascending
                : Octokit.SortDirection.Descending;
        }

        // Set the page and page size if provided
        var options = new ApiOptions();
        if (page.HasValue)
            options.StartPage = page.Value;

        if (pageSize.HasValue)
            options.PageSize = pageSize.Value;

        var milestones = await client.Issue.Milestone.GetAllForRepository(owner, repository, request, options);
        context.Set(Milestones, milestones);
    }
}