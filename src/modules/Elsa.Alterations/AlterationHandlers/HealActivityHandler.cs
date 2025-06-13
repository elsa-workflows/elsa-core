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
public class HealActivityHandler : AlterationHandlerBase<HealActivity>
{
    /// <inheritdoc />
    protected override ValueTask HandleAsync(AlterationContext context, HealActivity alteration)
    {
        var activityExecutionContexts = context.WorkflowExecutionContext.FindActivityExecutionContexts(alteration.ActivityHandle).ToList();

        if (!activityExecutionContexts.Any())
        {
            context.Fail($"Activity execution context with handle {alteration.ActivityHandle} not found");

            return ValueTask.CompletedTask;
        }

        context.Succeed(() => Heal(activityExecutionContexts));
        return ValueTask.CompletedTask;
    }

    private void Heal(IEnumerable<ActivityExecutionContext> activityExecutionContexts)
    {
        foreach (var activityExecutionContext in activityExecutionContexts)
            Heal(activityExecutionContext);
    }

    private void Heal(ActivityExecutionContext activityExecutionContext)
    {
        activityExecutionContext.RecoverFromFault();
    }
}