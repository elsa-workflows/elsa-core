using Elsa.Contracts;
using Elsa.Models;
using Elsa.Signals;

namespace Elsa.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class BreakBehavior : Behavior
{
    public BreakBehavior()
    {
        OnSignalReceived<BreakSignal>(OnBreakAsync);
    }

    private async ValueTask OnBreakAsync(BreakSignal signal, SignalContext context)
    {
        // Prevent bubbling.
        context.StopPropagation();

        // Remove child activity execution contexts.
        context.ActivityExecutionContext.RemoveChildren();

        // Mark this activity as completed.
        await context.ActivityExecutionContext.CompleteActivityAsync();
    }
}