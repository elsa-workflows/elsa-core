using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Triggers when a user is added or changed.
/// </summary>
[Activity(
    "Elsa.Slack.Events",
    "Slack Events",
    "Triggers when a user is added or changed.",
    DisplayName = "Watch Users")]
[UsedImplicitly]
public class WatchUsers : SlackEventActivity
{
    /// <summary>
    /// The user that was added or changed.
    /// </summary>
    [Output(Description = "The user that was added or changed.")]
    public Output<User> ChangedUser { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Slack's Events API and WebSocket support
        throw new NotImplementedException("Event subscription requires WebSocket implementation.");
    }
}