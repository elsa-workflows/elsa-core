using Elsa.Contracts;
using Elsa.Models;
using Elsa.Signals;

namespace Elsa.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class BreakBehavior : IBehavior
{
    public async ValueTask HandleSignalAsync(object signal, SignalContext context)
    {
        if (signal is not BreakSignal)
            return;

        await OnBreakAsync(context);
    }

    public ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;

    private async ValueTask OnBreakAsync(SignalContext context)
    {
        // Prevent bubbling.
        context.StopPropagation();

        // Remove child activity execution contexts.
        context.ActivityExecutionContext.RemoveChildren();

        // Mark this activity as completed.
        await context.ActivityExecutionContext.CompleteActivityAsync();
    }
}