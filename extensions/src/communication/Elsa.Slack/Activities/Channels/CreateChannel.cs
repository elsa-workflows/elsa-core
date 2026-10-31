using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Creates a new channel in a Slack workspace.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Creates a new channel in the workspace.",
    DisplayName = "Create Channel")]
[UsedImplicitly]
public class CreateChannel : SlackActivity
{
    /// <summary>
    /// The name of the channel to create.
    /// </summary>
    [Input(Description = "The name of the channel to create.")]
    public Input<string> ChannelName { get; set; } = null!;

    /// <summary>
    /// Whether to create the channel as private.
    /// </summary>
    [Input(Name = "Is Private", Description = "Whether to create the channel as private.")]
    public Input<bool> IsPrivate { get; set; } = null!;

    /// <summary>
    /// The ID of the workspace.
    /// </summary>
    [Input(Name = "Team Id", Description = "The ID of the workspace.")]
    public Input<string>? TeamId { get; set; }

    /// <summary>
    /// The created channel information.
    /// </summary>
    [Output(Description = "The created channel information.")]
    public Output<Conversation> Channel { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelName = context.Get(ChannelName)!;
        bool isPrivate = context.Get(IsPrivate);
        string? teamId = context.Get(TeamId);

        ISlackApiClient client = GetClient(context);
        Conversation channel = await client.Conversations.Create(channelName, isPrivate, teamId);

        context.Set(Channel, channel);
    }
}