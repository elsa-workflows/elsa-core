using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Users;

/// <summary>
/// Sets a user's status.
/// </summary>
[Activity(
    "Elsa.Slack.Users",
    "Slack Users",
    "Sets a user's status.",
    DisplayName = "Set Status")]
[UsedImplicitly]
public class SetStatus: SlackActivity
{
    /// <summary>
    /// The status text to set.
    /// </summary>
    [Input(Description = "The status text to set.")]
    public Input<string> StatusText { get; set; } = null!;

    /// <summary>
    /// The emoji to use for the status.
    /// </summary>
    [Input(Description = "The emoji to use for the status.")]
    public Input<string> StatusEmoji { get; set; } = null!;

    /// <summary>
    /// Optional Unix timestamp for when the status should expire.
    /// </summary>
    [Input(Description = "Optional Unix timestamp for when the status should expire.")]
    public Input<long> StatusExpiration { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string statusText = context.Get(StatusText)!;
        string statusEmoji = context.Get(StatusEmoji)!;
        long? statusExpiration = context.Get(StatusExpiration);

        ISlackApiClient client = GetClient(context);

        await client.UserProfile.Set(new UserProfile
        {
            StatusText = statusText,
            StatusEmoji = statusEmoji,
            StatusExpiration = statusExpiration ?? 0
        });
    }
}