using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Sets the topic of a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Sets the topic for a channel.",
    DisplayName = "Set Channel Topic")]
[UsedImplicitly]
public class SetChannelTopic : SlackActivity
{
    /// <summary>
    /// The ID of the channel to set the topic of.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the channel to set the topic of.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// The new topic.
    /// </summary>
    [Input(Description = "The new topic.")]
    public Input<string> Topic { get; set; } = null!;

    /// <summary>
    /// The updated topic.
    /// </summary>
    [Output(Description = "The updated topic.")]
    public Output<string> UpdatedTopic { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        string topic = context.Get(Topic)!;

        ISlackApiClient client = GetClient(context);
        string updatedTopic = await client.Conversations.SetTopic(channelId, topic);

        context.Set(UpdatedTopic, updatedTopic);
    }
}