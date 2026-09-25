using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reminders;

/// <summary>
/// Retrieves information about a reminder.
/// </summary>
[Activity(
    "Elsa.Slack.Reminders",
    "Slack Reminders",
    "Gets information about a specific reminder.",
    DisplayName = "Get Reminder")]
[UsedImplicitly]
public class GetReminder : SlackActivity
{
    /// <summary>
    /// The ID of the reminder to get information about.
    /// </summary>
    [Input(Name = "Reminder Id", Description = "The ID of the reminder to get information about.")]
    public Input<string> ReminderId { get; set; } = null!;

    /// <summary>
    /// The reminder information.
    /// </summary>
    [Output(Name = "Reminder Info", Description = "The reminder information.")]
    public Output<Reminder> ReminderInfo { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string reminderId = context.Get(ReminderId)!;

        ISlackApiClient client = GetClient(context);
        Reminder reminder = await client.Reminders.Info(reminderId);
        context.Set(ReminderInfo, reminder);
    }
}