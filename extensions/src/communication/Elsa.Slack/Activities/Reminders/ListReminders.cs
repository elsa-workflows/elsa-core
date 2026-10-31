using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reminders;

/// <summary>
/// Lists all reminders created by or for a given user.
/// </summary>
[Activity(
    "Elsa.Slack.Reminders",
    "Slack Reminders",
    "Lists all reminders for a user.",
    DisplayName = "List Reminders")]
[UsedImplicitly]
public class ListReminders : SlackActivity
{
    /// <summary>
    /// The user to list reminders for. If not specified, defaults to the authed user.
    /// </summary>
    [Input(Name = "User Id", Description = "The user to list reminders for. If not specified, defaults to the authed user.")]
    public Input<string>? UserId { get; set; }

    /// <summary>
    /// The list of reminders.
    /// </summary>
    [Output(Description = "The list of reminders.")]
    public Output<IReadOnlyList<Reminder>> Reminders { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string? userId = context.Get(UserId);

        ISlackApiClient client = GetClient(context);
        IReadOnlyList<Reminder>? response = await client.Reminders.List();
        context.Set(Reminders, response);
    }
}