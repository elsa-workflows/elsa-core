using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Gists;

/// <summary>
/// Retrieves a GitHub Gist without colliding with the workflow activity ID.
/// </summary>
[Activity(
    "Elsa.GitHub.Gists",
    "GetGist",
    2,
    "Retrieves a GitHub Gist by its ID.",
    "GitHub Gists",
    DisplayName = "Get Gist")]
[UsedImplicitly]
public class GetGistV2 : GitHubActivity
{
    public GetGistV2()
    {
        Version = 2;
    }

    /// <summary>
    /// The provider ID of the Gist to retrieve.
    /// </summary>
    [Input(Description = "The ID of the Gist to retrieve.")]
    public Input<string> GistId { get; set; } = null!;

    /// <summary>
    /// The retrieved Gist.
    /// </summary>
    [Output(Description = "The retrieved Gist.")]
    public Output<Gist> RetrievedGist { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var gistId = context.Get(GistId)!;
        var client = GetClient(context);
        var gist = await client.Gist.Get(gistId);

        context.Set(RetrievedGist, gist);
    }
}
