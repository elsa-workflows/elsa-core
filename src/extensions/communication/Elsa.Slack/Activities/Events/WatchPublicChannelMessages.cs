using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Triggers when a message is added to a public channel.
/// </summary>
[Activity(
    "Elsa.Slack.Events",
    "Slack Events",
    "Triggers when a message is added to a public channel.",
    DisplayName = "Watch Public Channel Messages")]
[UsedImplicitly]
public class WatchPublicChannelMessages : SlackEventActivity
{
    /// <summary>
    /// The ID of the public channel to watch.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the public channel to watch.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// The received message.
    /// </summary>
    [Output(Description = "The received message.")]
    public Output<Message> ReceivedMessage { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Slack's Events API and WebSocket support
        throw new NotImplementedException("Event subscription requires WebSocket implementation.");
    }
}