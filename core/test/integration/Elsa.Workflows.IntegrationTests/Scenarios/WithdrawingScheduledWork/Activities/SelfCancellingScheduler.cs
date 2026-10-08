using Elsa.Extensions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WithdrawingScheduledWork.Activities;

/// <summary>
/// A container that schedules a child the ordinary way — by activity, so the child has no execution context yet — and
/// then cancels itself. There is no child context to cancel recursively, so withdrawing the queued work item is the
/// only thing that can stop the child from running under a container that no longer exists.
/// </summary>
public class SelfCancellingScheduler : Activity
{
    public IActivity Child { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.ScheduleActivityAsync(Child);
        await context.CancelActivityAsync();
    }
}
