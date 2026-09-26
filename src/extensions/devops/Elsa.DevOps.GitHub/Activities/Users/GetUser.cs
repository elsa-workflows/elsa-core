using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Users;

/// <summary>
/// Retrieves details of a GitHub user.
/// </summary>
[Activity(
    "Elsa.GitHub.Users",
    "GitHub Users",
    "Retrieves details of a GitHub user.",
    DisplayName = "Get User")]
[UsedImplicitly]
public class GetUser : GitHubActivity
{
    /// <summary>
    /// The username of the user to retrieve.
    /// </summary>
    [Input(Description = "The username of the user to retrieve.")]
    public Input<string> Username { get; set; } = null!;

    /// <summary>
    /// The retrieved user.
    /// </summary>
    [Output(Description = "The retrieved user.")]
    public Output<User> RetrievedUser { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var username = context.Get(Username)!;

        var client = GetClient(context);
        var user = await client.User.Get(username);

        context.Set(RetrievedUser, user);
    }
}