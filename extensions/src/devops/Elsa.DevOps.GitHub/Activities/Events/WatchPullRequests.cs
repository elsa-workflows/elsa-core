using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Events;

/// <summary>
/// Triggers when a pull request is created or updated.
/// </summary>
[Activity(
    "Elsa.GitHub.Events",
    "GitHub Events",
    "Triggers when a pull request is created or updated.",
    DisplayName = "Watch Pull Requests")]
[UsedImplicitly]
public class WatchPullRequests : GitHubEventActivity
{
    /// <summary>
    /// The owner of the repository to watch.
    /// </summary>
    [Input(Description = "The owner of the repository to watch.")]
    public Input<string> Owner { get; set; } = null!;

    /// <summary>
    /// The name of the repository to watch.
    /// </summary>
    [Input(Description = "The name of the repository to watch.")]
    public Input<string> Repository { get; set; } = null!;

    /// <summary>
    /// The pull request event that triggered the activity.
    /// </summary>
    [Output(Description = "The pull request event that triggered the activity.")]
    public Output<PullRequest> PullRequestEvent { get; set; } = null!;
}