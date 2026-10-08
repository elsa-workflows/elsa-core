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
    /// <summary>The legacy path remains the default for definitions without this input.</summary>
    [Input(Description = "Legacy token mode or explicitly admitted public message data.")]
    public Input<WatchPublicChannelMessageMode> Mode { get; set; } = new(WatchPublicChannelMessageMode.LegacyToken);

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

    [Output(Description = "The original incoming message timestamp.")]
    public Output<string> MessageTimestamp { get; set; } = null!;

    [Output(Description = "The original message sender.")]
    public Output<string> UserId { get; set; } = null!;

    [Output(Description = "The incoming thread timestamp, or the original message timestamp for a new thread.")]
    public Output<string> ReplyThreadTimestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var mode = context.Get(Mode);
        if (mode == WatchPublicChannelMessageMode.LegacyToken)
        {
            throw new NotImplementedException("Event subscription requires WebSocket implementation.");
        }
        if (mode != WatchPublicChannelMessageMode.AdmittedPublicMessage || !string.IsNullOrEmpty(context.Get(Token)))
        {
            throw new InvalidOperationException("Admitted public messages require an explicit mode and an empty Token.");
        }
        var message = await context.GetRequiredService<ISlackPublicChannelMessageSource>().ReadAsync(context, context.CancellationToken);
        var channelId = context.Get(ChannelId);
        var botUserId = context.Get(BotUserId);
        if (channelId != message.ChannelId || !string.IsNullOrEmpty(botUserId) && botUserId != message.SelfUserId)
        {
            throw new InvalidOperationException("The Watch inputs do not match the trusted public-channel route.");
        }
        // Keep the original SlackNet.WebApi.Message contract. Incoming sender/timestamp are
        // separate scalar outputs; ThreadTs remains the incoming thread value, not a reply default.
        context.Set(ReceivedMessage, new Message { Channel = message.ChannelId, Text = message.Text, ThreadTs = message.ThreadTimestamp });
        context.Set(MessageTimestamp, message.MessageTimestamp);
        context.Set(UserId, message.UserId);
        context.Set(ReplyThreadTimestamp, message.ReplyThreadTimestamp);
    }
}
