using Elsa.Slack.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reactions;

/// <summary>
/// Removes a reaction from a message.
/// </summary>
[Activity(
    "Elsa.Slack.Reactions",
    "Slack Reactions",
    "Removes a reaction from a message.",
    DisplayName = "Remove Reaction")]
[UsedImplicitly]
public class RemoveReaction : SlackActivity
{
    /// <summary>
    /// The name of the emoji to remove.
    /// </summary>
    [Input(Description = "The name of the emoji to remove.")]
    public Input<string> Emoji { get; set; } = null!;

    /// <summary>
    /// The channel containing the message.
    /// </summary>
    [Input(Description = "The channel containing the message.")]
    public Input<string> Channel { get; set; } = null!;

    /// <summary>
    /// The timestamp of the message.
    /// </summary>
    [Input(Description = "The timestamp of the message.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string emoji = context.Get(Emoji)!;
        string channel = context.Get(Channel)!;
        string timestamp = context.Get(Timestamp)!;
        ISlackApiClient client = GetClient(context);
        await client.Reactions.RemoveFromMessage(emoji, channel, timestamp);
    }
}