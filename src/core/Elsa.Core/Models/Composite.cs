using Elsa.Activities.Workflows;
using Elsa.Attributes;
using Elsa.Contracts;

namespace Elsa.Models;

public abstract class Composite : Activity
{
    [Outbound] public IActivity Root { get; protected set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.ScheduleActivity(Root, OnCompletedAsync);
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;
}