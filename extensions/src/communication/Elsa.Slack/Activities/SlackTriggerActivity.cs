using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Slack.Activities;

/// <summary>
/// Base class for Slack event trigger activities.
/// </summary>
public abstract class SlackTriggerActivity : SlackActivity, ITrigger
{
    /// <summary>
    /// The ID of the bot user to filter out self-messages.
    /// </summary>
    [Input(Name = "Bot User Id", Description = "The ID of the bot user to filter out self-messages.")]
    public Input<string> BotUserId { get; set; } = null!;

    /// <summary>
    /// The ID of the channel to listen for messages in.
    /// </summary>
    public abstract string GetTriggerType();

    /// <summary>
    /// Returns the payloads to index.
    /// </summary>
    /// <param name="context">The trigger indexing context.</param>
    public abstract ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context);
}