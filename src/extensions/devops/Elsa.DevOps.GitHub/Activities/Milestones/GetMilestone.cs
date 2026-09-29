using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Milestones;

/// <summary>
/// Retrieves details of a specific milestone from a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Milestones",
    "GitHub Milestones",
    "Retrieves details of a specific milestone from a GitHub repository.",
    DisplayName = "Get Milestone")]
[UsedImplicitly]
public class GetMilestone : GitHubActivity
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
    /// The milestone number.
    /// </summary>
    [Input(Description = "The milestone number.")]
    public Input<int> Number { get; set; } = null!;

    /// <summary>
    /// The retrieved milestone.
    /// </summary>
    [Output(Description = "The retrieved milestone.")]
    public Output<Milestone> RetrievedMilestone { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);

        var client = GetClient(context);
        var milestone = await client.Issue.Milestone.Get(owner, repository, number);

        context.Set(RetrievedMilestone, milestone);
    }
}