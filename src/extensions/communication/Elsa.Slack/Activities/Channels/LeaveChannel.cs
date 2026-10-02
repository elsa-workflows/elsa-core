using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Leaves a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Leaves a channel.",
    DisplayName = "Leave Channel")]
[UsedImplicitly]
public class LeaveChannel : SlackActivity
{
    /// <summary>
    /// The ID of the channel to leave.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the channel to leave.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;

        ISlackApiClient client = GetClient(context);
        await client.Conversations.Leave(channelId);
    }
}