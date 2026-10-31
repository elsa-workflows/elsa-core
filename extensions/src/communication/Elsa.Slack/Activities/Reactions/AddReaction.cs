using Elsa.Slack.Services;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reactions;

/// <summary>
/// Adds a reaction to a message.
/// </summary>
[Activity(
    "Elsa.Slack.Reactions",
    "Slack Reactions",
    "Adds a reaction to a message.",
    DisplayName = "Add Reaction")]
[UsedImplicitly]
public class AddReaction : SlackActivity
{
    /// <summary>
    /// The name of the emoji to react with.
    /// </summary>
    [Input(Description = "The name of the emoji to react with.")]
    public Input<string> Emoji { get; set; } = null!;

    /// <summary>
    /// The channel containing the message.
    /// </summary>
    [Input(Description = "The channel containing the message.")]
    public Input<string> Channel { get; set; } = null!;

    /// <summary>
    /// The timestamp of the message to react to.
    /// </summary>
    [Input(Description = "The timestamp of the message to react to.")]
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
        await client.Reactions.AddToMessage(emoji, channel, timestamp);
    }
}