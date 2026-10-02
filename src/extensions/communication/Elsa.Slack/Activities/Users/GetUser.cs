using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Users;

/// <summary>
/// Retrieves details about a member of a workspace.
/// </summary>
[Activity(
    "Elsa.Slack.Users",
    "Slack Users",
    "Retrieves details about a member of a workspace.",
    DisplayName = "Get User")]
[UsedImplicitly]
public class GetUser : SlackActivity
{
    /// <summary>
    /// The ID of the user to get information about.
    /// </summary>
    [Input(Description = "The ID of the user to get information about.")]
    public Input<string> UserId { get; set; } = null!;

    /// <summary>
    /// The user information.
    /// </summary>
    [Output(Description = "The user information.")]
    public Output<User> UserInfo { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string userId = context.Get(UserId)!;

        ISlackApiClient client = GetClient(context);
        User user = await client.Users.Info(userId);
        context.Set(UserInfo, user);
    }
}