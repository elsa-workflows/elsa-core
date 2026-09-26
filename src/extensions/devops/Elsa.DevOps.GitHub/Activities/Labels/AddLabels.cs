using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Labels;

/// <summary>
/// Adds labels to an issue or pull request in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Labels",
    "GitHub Labels",
    "Adds labels to an issue or pull request in a GitHub repository.",
    DisplayName = "Add Labels")]
[UsedImplicitly]
public class AddLabels : GitHubActivity
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
    /// The issue or pull request number.
    /// </summary>
    [Input(Description = "The issue or pull request number.")]
    public Input<int> Number { get; set; } = null!;

    /// <summary>
    /// The labels to add.
    /// </summary>
    [Input(Description = "The labels to add.")]
    public Input<IEnumerable<string>> Labels { get; set; } = null!;

    /// <summary>
    /// The applied labels.
    /// </summary>
    [Output(Description = "The applied labels.")]
    public Output<IReadOnlyList<Label>> AppliedLabels { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);
        var labels = context.Get(Labels)!;

        var client = GetClient(context);
        var appliedLabels = await client.Issue.Labels.AddToIssue(owner, repository, number, labels.ToArray());

        context.Set(AppliedLabels, appliedLabels);
    }
}