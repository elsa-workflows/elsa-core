using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Users;

/// <summary>
/// Lists all users in a workspace.
/// </summary>
[Activity(
    "Elsa.Slack.Users",
    "Slack Users",
    "Lists all users in a workspace.",
    DisplayName = "List Users")]
[UsedImplicitly]
public class ListUsers : SlackActivity
{
    /// <summary>
    /// The list of users in the workspace.
    /// </summary>
    [Output(Description = "The list of users in the workspace.")]
    public Output<IList<User>> Users { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string token = context.Get(Token)!;

        ISlackApiClient client = GetClient(context);
        UserListResponse users = await client.Users.List();
        context.Set(Users, users.Members);
    }
}