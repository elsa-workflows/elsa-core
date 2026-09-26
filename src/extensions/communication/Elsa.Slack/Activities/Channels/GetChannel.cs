using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Retrieves information about a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Gets information about a channel.",
    DisplayName = "Get Channel")]
[UsedImplicitly]
public class GetChannel : SlackActivity
{
    /// <summary>
    /// The ID of the channel to get information about.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the channel to get information about.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// The channel information.
    /// </summary>
    [Output(Description = "The channel information.")]
    public Output<Conversation> Channel { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;

        ISlackApiClient client = GetClient(context);
        Conversation channel = await client.Conversations.Info(channelId);
        context.Set(Channel, channel);
    }
}