using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Activities;
using JetBrains.Annotations;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Triggers when any new event is created.
/// </summary>
[Activity(
    "Elsa.Slack.Events",
    "Slack Events",
    "Triggers when any new event is created.",
    DisplayName = "Watch New Events")]
[UsedImplicitly]
public class WatchNewEvents : SlackEventActivity
{
    /// <summary>
    /// The received event.
    /// </summary>
    [Output(Description = "The received event.")]
    public Output<Event> ReceivedEvent { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Slack's Events API and WebSocket support
        throw new NotImplementedException("Event subscription requires WebSocket implementation.");
    }
}