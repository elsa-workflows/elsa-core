using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Messages;

/// <summary>
/// Deletes a message from a Slack channel.
/// </summary>
[Activity(
    "Elsa.Slack.Messages",
    "Slack Messages",
    "Deletes a message from a Slack channel.",
    DisplayName = "Delete Message")]
[UsedImplicitly]
public class DeleteMessage : SlackActivity
{
    /// <summary>
    /// The channel containing the message.
    /// </summary>
    [Input(Description = "The channel containing the message.")]
    public Input<string> Channel { get; set; } = null!;

    /// <summary>
    /// The timestamp of the message to delete.
    /// </summary>
    [Input(Description = "The timestamp of the message to delete.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channel = context.Get(Channel)!;
        string ts = context.Get(Timestamp)!;

        ISlackApiClient client = GetClient(context);

        await client.Chat.Delete(channel, ts);
    }
}