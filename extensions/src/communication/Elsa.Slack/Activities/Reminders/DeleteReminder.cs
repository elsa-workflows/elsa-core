using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reminders;

/// <summary>
/// Deletes a reminder.
/// </summary>
[Activity(
    "Elsa.Slack.Reminders",
    "Slack Reminders",
    "Deletes a reminder.",
    DisplayName = "Delete Reminder")]
[UsedImplicitly]
public class DeleteReminder : SlackActivity
{
    /// <summary>
    /// The ID of the reminder to delete.
    /// </summary>
    [Input(Name = "Reminder Id", Description = "The ID of the reminder to delete.")]
    public Input<string> ReminderId { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string reminderId = context.Get(ReminderId)!;

        ISlackApiClient client = GetClient(context);
        await client.Reminders.Delete(reminderId, CancellationToken.None);
    }
}