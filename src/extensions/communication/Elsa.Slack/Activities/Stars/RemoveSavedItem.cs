using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Stars;

/// <summary>
/// Removes an item from saved items.
/// </summary>
[Activity(
    "Elsa.Slack.SavedItems",
    "Slack Saved Items",
    "Removes a saved item.",
    DisplayName = "Remove Saved Item")]
[UsedImplicitly]
public class RemoveSavedItem : SlackActivity
{
    /// <summary>
    /// Channel where the saved item is located.
    /// </summary>
    [Input(Name = "Channel Id", Description = "Channel where the saved item is located.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// Timestamp of the message to remove from saved items.
    /// </summary>
    [Input(Description = "Timestamp of the message to remove from saved items.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        string timestamp = context.Get(Timestamp)!;

        ISlackApiClient client = GetClient(context);
        await client.Pins.RemoveMessage(channelId, timestamp);
    }
}