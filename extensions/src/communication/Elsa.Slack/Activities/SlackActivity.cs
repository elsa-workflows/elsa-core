using Elsa.Slack.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using SlackNet;

namespace Elsa.Slack.Activities;

/// <summary>
/// Generic base class inherited by all Slack activities.
/// </summary>
public abstract class SlackActivity : Activity
{
    /// <summary>
    /// The Slack API token.
    /// </summary>
    [Input(Description = "The Slack API token.")]
    public Input<string> Token { get; set; } = null!;

    /// <summary>
    /// Gets the Slack API client.
    /// </summary>
    /// <param name="context">The current context to get the client.</param>
    /// <returns>The Slack API client.</returns>
    protected ISlackApiClient GetClient(ActivityExecutionContext context)
    {
        SlackClientFactory slackClientFactory = context.GetRequiredService<SlackClientFactory>();
        string token = context.Get(Token)!;
        return slackClientFactory.GetClient(token);
    }
}