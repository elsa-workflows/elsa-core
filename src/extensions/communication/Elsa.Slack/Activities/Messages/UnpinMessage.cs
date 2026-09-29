using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Messages;

/// <summary>
/// Unpins a message from a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Messages",
    "Slack Messages",
    "Unpins a message from a channel.",
    DisplayName = "Unpin Message")]
[UsedImplicitly]
public class UnpinMessage : SlackActivity
{
    /// <summary>
    /// The channel containing the message.
    /// </summary>
    [Input(Description = "The channel containing the message.")]
    public Input<string> Channel { get; set; } = null!;

    /// <summary>
    /// The timestamp of the message to unpin.
    /// </summary>
    [Input(Description = "The timestamp of the message to unpin.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channel = context.Get(Channel)!;
        string ts = context.Get(Timestamp)!;

        ISlackApiClient client = GetClient(context);
        await client.Pins.RemoveMessage(channel, ts);
    }
}