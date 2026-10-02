using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Users;

/// <summary>
/// Checks if a user can be assigned to issues in a repository.
/// </summary>
[Activity(
    "Elsa.GitHub.Users",
    "GitHub Users",
    "Checks if a user can be assigned to issues in a repository.",
    DisplayName = "Get Assignee")]
[UsedImplicitly]
public class GetAssignee : GitHubActivity
{
    /// <summary>
    /// The owner of the repository.
    /// </summary>
    [Input(Description = "The owner of the repository.")]
    public Input<string> Owner { get; set; } = null!;

    /// <summary>
    /// The name of the repository.
    /// </summary>
    [Input(Description = "The name of the repository.")]
    public Input<string> Repository { get; set; } = null!;

    /// <summary>
    /// The username to check.
    /// </summary>
    [Input(Description = "The username to check.")]
    public Input<string> Username { get; set; } = null!;

    /// <summary>
    /// The assignee user if available.
    /// </summary>
    [Output(Description = "The assignee user if available.")]
    public Output<User?> AssigneeUser { get; set; } = null!;

    /// <summary>
    /// Whether the user can be assigned.
    /// </summary>
    [Output(Description = "Whether the user can be assigned.")]
    public Output<bool> CanBeAssigned { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var owner = context.Get(Owner)!;
        var repository = context.Get(Repository)!;
        var username = context.Get(Username)!;

        var client = GetClient(context);
        
        try
        {
            var assignee = await client.Issue.Assignee.CheckAssignee(owner, repository, username);
            context.Set(CanBeAssigned, assignee);
            
            if (assignee)
            {
                var user = await client.User.Get(username);
                context.Set(AssigneeUser, user);
            }
            else
            {
                context.Set(AssigneeUser, null);
            }
        }
        catch (NotFoundException)
        {
            context.Set(CanBeAssigned, false);
            context.Set(AssigneeUser, null);
        }
    }
}