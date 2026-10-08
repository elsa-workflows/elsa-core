using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Stars;

/// <summary>
/// Adds an item to saved items.
/// </summary>
[Activity(
    "Elsa.Slack.SavedItems",
    "Slack Saved Items",
    "Saves an item for later reference.",
    DisplayName = "Save Item")]
[UsedImplicitly]
public class SaveItem : SlackActivity
{
    /// <summary>
    /// Channel where the item is located.
    /// </summary>
    [Input(Name = "Channel Id", Description = "Channel where the item is located.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// Timestamp of the message to save.
    /// </summary>
    [Input(Description = "Timestamp of the message to save.")]
    public Input<string> Timestamp { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        string timestamp = context.Get(Timestamp)!;

        ISlackApiClient client = GetClient(context);
        await client.Pins.AddMessage(channelId, timestamp);
    }
}