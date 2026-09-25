using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Users;

/// <summary>
/// Searches for a user by email address.
/// </summary>
[Activity(
    "Elsa.Slack.Users",
    "Slack Users",
    "Searches for a user by email address.",
    DisplayName = "Search User By Email")]
[UsedImplicitly]
public class SearchForUser : SlackActivity
{
    /// <summary>
    /// The email address to search for.
    /// </summary>
    [Input(Description = "The email address to search for.")]
    public Input<string> Email { get; set; } = null!;

    /// <summary>
    /// The found user information.
    /// </summary>
    [Output(Description = "The found user information.")]
    public Output<User> FoundUser { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string email = context.Get(Email)!;

        ISlackApiClient client = GetClient(context);
        User user = await client.Users.LookupByEmail(email);
        context.Set(FoundUser, user);
    }
}