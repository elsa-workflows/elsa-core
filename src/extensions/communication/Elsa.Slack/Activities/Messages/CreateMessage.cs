using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Messages;

/// <summary>
/// Sends a new message to a Slack channel.
/// </summary>
[Activity(
    "Elsa.Slack.Messages",
    "Slack Messages",
    "Sends a new message to a Slack channel.",
    DisplayName = "Send Message")]
[UsedImplicitly]
public class CreateMessage : SlackActivity
{
    /// <summary>
    /// The channel to send the message to.
    /// </summary>
    [Input(Description = "The channel to send the message to.")]
    public Input<string> Channel { get; set; } = null!;

    /// <summary>
    /// The message text to send.
    /// </summary>
    [Input(Description = "The message text to send.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// Optional thread timestamp to reply to a thread.
    /// </summary>
    [Input(Description = "Optional thread timestamp to reply to a thread.")]
    public Input<string?> ThreadTimestamp { get; set; } = null!;

    /// <summary>
    /// Whether to broadcast a reply to a thread to the channel.
    /// </summary>
    [Input(Description = "Whether to broadcast a reply to a thread to the channel.")]
    public Input<bool> ReplyBroadcast { get; set; } = null!;

    /// <summary>
    /// The timestamp of the sent message.
    /// </summary>
    [Output(Description = "The timestamp of the sent message.")]
    public Output<string?> MessageTimestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channel = context.Get(Channel)!;
        string text = context.Get(Text)!;
        string? threadTs = context.Get(ThreadTimestamp);
        bool replyBroadcast = context.Get(ReplyBroadcast);

        ISlackApiClient client = GetClient(context);

        Message message = new()
        {
            Channel = channel,
            Text = text,
            ThreadTs = threadTs,
            ReplyBroadcast = replyBroadcast
        };

        PostMessageResponse? response = await client.Chat.PostMessage(message);

        context.Set(MessageTimestamp, response?.Ts);
    }
}