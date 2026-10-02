using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;

namespace Elsa.Slack.Activities.Reminders;

/// <summary>
/// Creates a reminder.
/// </summary>
[Activity(
    "Elsa.Slack.Reminders",
    "Slack Reminders",
    "Creates a new reminder.",
    DisplayName = "Create Reminder")]
[UsedImplicitly]
public class CreateReminder : SlackActivity
{
    /// <summary>
    /// The text of the reminder.
    /// </summary>
    [Input(Description = "The text of the reminder.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// When this reminder should happen: the Unix timestamp (up to 5 years from now), or a natural language description (e.g. 'in 15 minutes' or 'every Thursday at 3pm').
    /// </summary>
    [Input(Description = "When this reminder should happen: the Unix timestamp (up to 5 years from now), or a natural language description (e.g. 'in 15 minutes' or 'every Thursday at 3pm').")]
    public Input<string> Time { get; set; } = null!;

    /// <summary>
    /// The user who will receive this reminder. If not specified, defaults to the authed user.
    /// </summary>
    [Input(Name = "User Id", Description = "The user who will receive this reminder. If not specified, defaults to the authed user.")]
    public Input<string>? UserId { get; set; }

    /// <summary>
    /// The ID of the created reminder.
    /// </summary>
    [Output(Name = "Reminder Id", Description = "The ID of the created reminder.")]
    public Output<string> ReminderId { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string text = context.Get(Text)!;
        string time = context.Get(Time)!;
        string? userId = context.Get(UserId);

        ISlackApiClient client = GetClient(context);
        Reminder reminder = await client.Reminders.Add(text, time, userId);
        context.Set(ReminderId, reminder.Id);
    }
}