using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Events;

/// <summary>
/// Triggers when an issue is created or updated.
/// </summary>
[Activity(
    "Elsa.GitHub.Events",
    "GitHub Events",
    "Triggers when an issue is created or updated.",
    DisplayName = "Watch Issues")]
[UsedImplicitly]
public class WatchIssues : GitHubEventActivity
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
    /// The issue event that triggered the activity.
    /// </summary>
    [Output(Description = "The issue event that triggered the activity.")]
    public Output<Issue> IssueEvent { get; set; } = null!;
}