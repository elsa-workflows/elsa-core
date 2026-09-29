using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Search;

/// <summary>
/// Searches for messages matching a query.
/// </summary>
[Activity(
    "Elsa.Slack.Search",
    "Slack Search",
    "Searches for messages matching a query.",
    DisplayName = "Search Messages")]
[UsedImplicitly]
public class SearchForMessage : SlackActivity
{
    /// <summary>
    /// The search query.
    /// </summary>
    [Input(Description = "Search query - can include modifiers like 'in:#channel', 'from:@user', etc.")]
    public Input<string> Query { get; set; } = null!;

    /// <summary>
    /// Number of results to return per page.
    /// </summary>
    [Input(Description = "Number of results to return per page.")]
    public Input<int?> Count { get; set; } = null!;

    /// <summary>
    /// Page number of results to return.
    /// </summary>
    [Input(Description = "Page number of results to return.")]
    public Input<int?> Page { get; set; } = null!;

    /// <summary>
    /// The search results.
    /// </summary>
    [Output(Description = "The search results.")]
    public Output<SearchResults<MessageSearchResult>> Results { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string query = context.Get(Query)!;
        int count = context.Get(Count) ?? 100;
        int page = context.Get(Page) ?? 1;

        ISlackApiClient client = GetClient(context);
        MessageSearchResponse results = await client.Search.Messages(query, count: count, page: page);
        context.Set(Results, results.Messages);
    }
}