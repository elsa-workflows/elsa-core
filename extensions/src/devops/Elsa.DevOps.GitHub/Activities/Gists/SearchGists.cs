using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Gists;

/// <summary>
/// Searches for GitHub Gists.
/// </summary>
[Activity(
    "Elsa.GitHub.Gists",
    "GitHub Gists",
    "Searches for GitHub Gists.",
    DisplayName = "Search Gists")]
[UsedImplicitly]
public class SearchGists : GitHubActivity
{
    /// <summary>
    /// The username to filter gists by (optional).
    /// </summary>
    [Input(Description = "The username to filter gists by (optional).")]
    public Input<string?> Username { get; set; } = null!;

    /// <summary>
    /// The since parameter to filter gists updated after a specific time (optional).
    /// </summary>
    [Input(Description = "The since parameter to filter gists updated after a specific time (optional).")]
    public Input<DateTimeOffset?> Since { get; set; } = null!;

    /// <summary>
    /// Whether to return only public gists or not.
    /// </summary>
    [Input(Description = "Whether to return only public gists or not.")]
    public Input<bool?> Public { get; set; } = null!;

    /// <summary>
    /// Whether to return only starred gists or not.
    /// </summary>
    [Input(Description = "Whether to return only starred gists or not.")]
    public Input<bool?> Starred { get; set; } = null!;

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
    /// The retrieved gists.
    /// </summary>
    [Output(Description = "The retrieved gists.")]
    public Output<IReadOnlyList<Gist>> Gists { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var username = context.Get(Username);
        var since = context.Get(Since);
        var isPublic = context.Get(Public);
        var isStarred = context.Get(Starred);
        var page = context.Get(Page);
        var pageSize = context.Get(PageSize);

        var client = GetClient(context);
        
        // Set the page and page size if provided
        var options = new ApiOptions();
        if (page.HasValue)
            options.StartPage = page.Value;

        if (pageSize.HasValue)
            options.PageSize = pageSize.Value;

        IReadOnlyList<Gist> gists;
        
        if (isStarred == true)
        {
            // Get starred gists
            gists = await client.Gist.GetAllStarred(options);
        }
        else if (!string.IsNullOrEmpty(username))
        {
            // Get user's gists
            gists = await client.Gist.GetAllForUser(username, options);
        }
        else
        {
            // Get all gists (or public gists)
            gists = isPublic == true 
                ? await client.Gist.GetAllPublic(options) 
                : await client.Gist.GetAll(options);
        }
        
        // Filter by since date if provided
        if (since.HasValue)
        {
            gists = gists.Where(g => g.UpdatedAt >= since.Value).ToList();
        }

        context.Set(Gists, gists);
    }
}