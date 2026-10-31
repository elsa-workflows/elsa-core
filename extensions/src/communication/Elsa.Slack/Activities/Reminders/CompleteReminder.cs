using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reminders;

/// <summary>
/// Marks a reminder as complete.
/// </summary>
[Activity(
    "Elsa.Slack.Reminders",
    "Slack Reminders",
    "Marks a reminder as complete.",
    DisplayName = "Complete Reminder")]
[UsedImplicitly]
public class CompleteReminder : SlackActivity
{
    /// <summary>
    /// The ID of the reminder to mark as complete.
    /// </summary>
    [Input(Name = "Reminder Id", Description = "The ID of the reminder to mark as complete.")]
    public Input<string> ReminderId { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string reminderId = context.Get(ReminderId)!;

        ISlackApiClient client = GetClient(context);
        await client.Reminders.Complete(reminderId);
    }
}