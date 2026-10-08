using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Events;

/// <summary>
/// Triggers when a new branch is created.
/// </summary>
[Activity(
    "Elsa.GitHub.Events",
    "GitHub Events",
    "Triggers when a new branch is created.",
    DisplayName = "Watch Branches")]
[UsedImplicitly]
public class WatchBranches : GitHubEventActivity
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
    /// The branch that triggered the activity.
    /// </summary>
    [Output(Description = "The branch that triggered the activity.")]
    public Output<Branch> BranchEvent { get; set; } = null!;
}