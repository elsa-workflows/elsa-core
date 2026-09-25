using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Kicks (removes) a user from a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Removes a user from a channel.",
    DisplayName = "Kick User From Channel")]
[UsedImplicitly]
public class KickUser : SlackActivity
{
    /// <summary>
    /// The ID of the channel to remove the user from.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the channel to remove the user from.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// The ID of the user to remove from channel.
    /// </summary>
    [Input(Name = "User Id", Description = "The ID of the user to remove from channel.")]
    public Input<string> UserId { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        string userId = context.Get(UserId)!;

        ISlackApiClient client = GetClient(context);
        await client.Conversations.Kick(channelId, userId);
    }
}