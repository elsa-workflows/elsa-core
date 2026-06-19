using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Represents a timer to periodically trigger the workflow.
/// </summary>
[Activity("Elsa", "Scheduling", "Trigger workflow execution at a specific interval using a CRON expression.")]
public class Cron : EventGenerator
{
    /// <inheritdoc />
    public Cron([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Cron(string cronExpression, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(new Input<string>(cronExpression), source, line)
    {
    }

    /// <inheritdoc />
    public Cron(Input<string> cronExpression, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        CronExpression = cronExpression;
    }

    /// <summary>
    /// The interval at which the timer should execute.
    /// </summary>
    [Input(Description = "The CRON expression at which the timer should execute.")]
    public Input<string> CronExpression { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(context.IsTriggerOfWorkflow())
        {
            await context.CompleteActivityAsync();
            return;
        }
        
        var cronExpression = context.ExpressionExecutionContext.Get(CronExpression);

        // Treat a blank expression as "disabled" so the activity completes without scheduling,
        // mirroring the trigger-indexing behavior, instead of throwing a CronFormatException at runtime.
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            await context.CompleteActivityAsync();
            return;
        }

        var cronParser = context.GetRequiredService<ICronParser>();
        var executeAt = cronParser.GetNextOccurrence(cronExpression);
        
        context.JournalData.Add("ExecuteAt", executeAt);
        context.CreateBookmark(new CronBookmarkPayload(executeAt, cronExpression));
    }

    /// <inheritdoc />
    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context)
    {
        // Treat a blank CRON expression as "no trigger" so the workflow can still be published
        // (e.g. to temporarily disable the schedule) instead of failing validation. A non-blank but
        // malformed expression still produces a payload and is caught by CronTriggerPayloadValidator.
        var cronExpression = context.ExpressionExecutionContext.Get(CronExpression);

        if (string.IsNullOrWhiteSpace(cronExpression))
            return [];

        return [new CronTriggerPayload(cronExpression)];
    }

    /// <summary>
    /// Creates a new <see cref="Cron"/> activity set to trigger at the specified cron expression.
    /// </summary>
    public static Cron FromCronExpression(string value, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) => new(value, source, line);
}