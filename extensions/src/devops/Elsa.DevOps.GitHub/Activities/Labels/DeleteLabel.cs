using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.DevOps.GitHub.Activities.Labels;

/// <summary>
/// Removes a label from an issue or pull request in a GitHub repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Labels",
    "GitHub Labels",
    "Removes a label from an issue or pull request in a GitHub repository.",
    DisplayName = "Delete Label")]
[UsedImplicitly]
public class DeleteLabel : GitHubActivity
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
    /// The label name to remove.
    /// </summary>
    [Input(Description = "The label name to remove.")]
    public Input<string> LabelName { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var number = context.Get(Number);
        var labelName = context.Get(LabelName)!;

        var client = GetClient(context);
        await client.Issue.Labels.RemoveFromIssue(owner, repository, number, labelName);
    }
}