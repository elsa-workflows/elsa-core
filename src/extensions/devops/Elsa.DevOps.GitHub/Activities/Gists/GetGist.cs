using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Gists;

/// <summary>
/// Retrieves a GitHub Gist by its ID.
/// </summary>
[Activity(
    "Elsa.GitHub.Gists",
    "GitHub Gists",
    "Retrieves a GitHub Gist by its ID.",
    DisplayName = "Get Gist")]
[UsedImplicitly]
public class GetGist : GitHubActivity
{
    /// <summary>
    /// The ID of the Gist to retrieve.
    /// </summary>
    [Input(Description = "The ID of the Gist to retrieve.")]
    public Input<string> Id { get; set; } = null!;

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
        var id = context.Get(Id)!;
        var client = GetClient(context);
        var gist = await client.Gist.Get(id);

        context.Set(RetrievedGist, gist);
    }
}