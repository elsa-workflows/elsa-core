using Elsa.Extensions;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WithdrawingScheduledWork.Activities;

/// <summary>
/// A container that schedules two children and then decides one of them must not run after all, before the engine has
/// taken either work item. It holds a handle on the speculative child by creating that child's execution context up
/// front and scheduling against it, which is how a container gets something to cancel later (Elsa.Bpmn does exactly
/// this when it starts a unit of work).
/// </summary>
public class SpeculativeScheduler : Activity
{
    /// <summary>The child that gets scheduled and then withdrawn.</summary>
    public IActivity Speculative { get; set; } = null!;

    /// <summary>The child that gets scheduled and left alone, so the run proves it got as far as executing children.</summary>
    public IActivity Committed { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var speculativeContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(Speculative, new ActivityInvocationOptions { Owner = context });
        workflowExecutionContext.AddActivityExecutionContext(speculativeContext);

        await context.ScheduleActivityAsync(Speculative, new ScheduleWorkOptions
        {
            ExistingActivityExecutionContext = speculativeContext,
            CompletionCallback = OnChildCompletedAsync
        });
        await context.ScheduleActivityAsync(Committed, new ScheduleWorkOptions
        {
            CompletionCallback = OnChildCompletedAsync
        });

        // The change of mind. Both work items are queued and neither has been invoked; the scheduler is FIFO, so the
        // speculative one is the very next thing the engine would take.
        await speculativeContext.CancelActivityAsync();
    }

    private async ValueTask OnChildCompletedAsync(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();
}
