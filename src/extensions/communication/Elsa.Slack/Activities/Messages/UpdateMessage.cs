using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Messages;

/// <summary>
/// Updates an existing message in a Slack channel.
/// </summary>
[Activity(
    "Elsa.Slack.Messages",
    "Slack Messages",
    "Updates an existing message in a Slack channel.",
    DisplayName = "Update Message")]
[UsedImplicitly]
public class UpdateMessage : SlackActivity
{
    /// <summary>
    /// The channel ID containing the message.
    /// </summary>
    [Input(Description = "The channel ID containing the message.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// The timestamp of the message to update.
    /// </summary>
    [Input(Description = "The timestamp of the message to update.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// The new text for the message.
    /// </summary>
    [Input(Description = "The new text for the message.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        string ts = context.Get(Timestamp)!;
        string text = context.Get(Text)!;

        ISlackApiClient client = GetClient(context);

        MessageUpdate message = new()
        {
            ChannelId = channelId,
            Text = text,
            Ts = ts
        };

        await client.Chat.Update(message);
    }
}