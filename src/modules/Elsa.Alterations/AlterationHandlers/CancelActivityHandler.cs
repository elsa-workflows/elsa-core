using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Cancels an activity.
/// </summary>
[UsedImplicitly]
public class CancelActivityHandler : AlterationHandlerBase<CancelActivity>
{
    /// <inheritdoc />
    protected override ValueTask HandleAsync(AlterationContext context, CancelActivity alteration)
    {
        if (alteration.ActivityInstanceId == null && alteration.ActivityId == null)
        {
            context.Fail("Either ActivityInstanceId or ActivityId must be specified");
            return ValueTask.CompletedTask;
        }

        var activityExecutionContexts = GetActivityExecutionContexts(context, alteration).ToList();

        if (!activityExecutionContexts.Any())
        {
            context.Fail(
                alteration.ActivityInstanceId != null
                    ? $"Activity execution context with ID {alteration.ActivityInstanceId} not found"
                    : $"Activity execution contexts for activity with ID {alteration.ActivityId} not found");

            return ValueTask.CompletedTask;
        }

        context.Succeed(() => CancelAsync(activityExecutionContexts));
        return ValueTask.CompletedTask;
    }

    private async Task CancelAsync(IEnumerable<ActivityExecutionContext> activityExecutionContexts)
    {
        foreach (var activityExecutionContext in activityExecutionContexts)
            await CancelAsync(activityExecutionContext);
    }

    private async Task CancelAsync(ActivityExecutionContext activityExecutionContext)
    {
        await activityExecutionContext.CancelActivityAsync();
    }

    private static IEnumerable<ActivityExecutionContext> GetActivityExecutionContexts(AlterationContext context, CancelActivity alteration)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        return alteration.ActivityInstanceId != null
            ? workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Id == alteration.ActivityInstanceId)
            : alteration.ActivityId != null
                ? workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Activity.Id == alteration.ActivityId)
                : Enumerable.Empty<ActivityExecutionContext>();
    }
}