using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Issues;

/// <summary>
/// Searches for issues in GitHub repositories.
/// </summary>
[Activity(
    "Elsa.GitHub.Issues",
    "GitHub Issues",
    "Searches for issues in GitHub repositories.",
    DisplayName = "Search Issues")]
[UsedImplicitly]
public class SearchIssues : GitHubActivity
{
    /// <summary>
    /// The search query.
    /// </summary>
    [Input(Description = "The search query following GitHub's issue search syntax.")]
    public Input<string> Query { get; set; } = null!;

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
    /// The sort field to order the results.
    /// </summary>
    [Input(Description = "The sort field to order the results.")]
    public Input<string?> Sort { get; set; } = null!;

    /// <summary>
    /// The direction to sort the results.
    /// </summary>
    [Input(Description = "The direction to sort the results (asc or desc).")]
    public Input<string?> SortDirection { get; set; } = null!;

    /// <summary>
    /// The search results.
    /// </summary>
    [Output(Description = "The search results.")]
    public Output<SearchIssuesResult> SearchResults { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var query = context.Get(Query)!;
        var page = context.Get(Page);
        var pageSize = context.Get(PageSize);
        var sort = context.Get(Sort);
        var sortDirection = context.Get(SortDirection);

        var client = GetClient(context);
        var searchRequest = new SearchIssuesRequest(query);

        if (page.HasValue)
            searchRequest.Page = page.Value;

        if (pageSize.HasValue)
            searchRequest.PerPage = pageSize.Value;

        if (!string.IsNullOrEmpty(sort))
            searchRequest.SortField = ParseSortField(sort);

        if (!string.IsNullOrEmpty(sortDirection))
            searchRequest.Order = ParseSortDirection(sortDirection);

        var results = await client.Search.SearchIssues(searchRequest);
        context.Set(SearchResults, results);
    }

    private static IssueSearchSort ParseSortField(string sort)
    {
        return sort.ToLowerInvariant() switch
        {
            "created" => IssueSearchSort.Created,
            "updated" => IssueSearchSort.Updated,
            "comments" => IssueSearchSort.Comments,
            _ => IssueSearchSort.Created
        };
    }

    private static SortDirection ParseSortDirection(string direction)
    {
        return direction.ToLowerInvariant() == "asc"
            ? Octokit.SortDirection.Ascending
            : Octokit.SortDirection.Descending;
    }
}