using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Reactions;

/// <summary>
/// Lists reactions made by a user.
/// </summary>
[Activity(
    "Elsa.Slack.Reactions",
    "Slack Reactions",
    "Lists reactions made by a user.",
    DisplayName = "List Reactions")]
[UsedImplicitly]
public class ListReactions : SlackActivity
{
    /// <summary>
    /// The user to list reactions for.
    /// </summary>
    [Input(Name = "User Id", Description = "The user to list reactions for.")]
    public Input<string> UserId { get; set; } = null!;

    /// <summary>
    /// The list of reactions made by the user.
    /// </summary>
    [Output(Name="User Reactions", Description="The list of reactions made by the user.")]
    public Output<IList<ReactionItem>> UserReactions { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string userId = context.Get(UserId)!;

        ISlackApiClient client = GetClient(context);
        ReactionItemListResponse? response = await client.Reactions.List(userId, full: true);
        context.Set(UserReactions, response?.Items);
    }
}