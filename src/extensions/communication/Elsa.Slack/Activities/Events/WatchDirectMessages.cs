using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Triggers when a direct message is received.
/// </summary>
[Activity(
    "Elsa.Slack.Events",
    "Slack Events",
    "Triggers when a direct message is received.",
    DisplayName = "Watch Direct Messages")]
[UsedImplicitly]
public class WatchDirectMessages : SlackEventActivity
{
    [Output(Description = "The received message.")]
    public Output<Message> ReceivedMessage { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Slack's Events API and WebSocket support
        throw new NotImplementedException("Event subscription requires WebSocket implementation.");
    }
}