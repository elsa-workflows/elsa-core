using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Slack.Activities.Events;

/// <summary>
/// Base class for Slack event watching activities.
/// </summary>
public abstract class SlackEventActivity : SlackActivity
{
    [Input(Name = "Bot User Id", Description = "The ID of the bot user to filter out self-messages.")]
    public Input<string> BotUserId { get; set; } = null!;
}