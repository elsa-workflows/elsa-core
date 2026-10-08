using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using File = SlackNet.File;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Triggers when a new file is added.
/// </summary>
[Activity(
    "Elsa.Slack.Events",
    "Slack Events",
    "Triggers when a new file is added.",
    DisplayName = "Watch Files")]
[UsedImplicitly]
public class WatchFiles : SlackEventActivity
{
    /// <summary>
    /// The added file.
    /// </summary>
    [Output(Description = "The added file.")]
    public Output<File> AddedFile { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Slack's Events API and WebSocket support
        throw new NotImplementedException("Event subscription requires WebSocket implementation.");
    }
}