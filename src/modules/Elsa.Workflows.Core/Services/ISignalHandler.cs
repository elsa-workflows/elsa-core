using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface ISignalHandler
{
    ValueTask HandleSignalAsync(object signal, SignalContext context);
}

public class SignalContext
{
    public SignalContext(ActivityExecutionContext activityExecutionContext, ActivityExecutionContext sourceActivityExecutionContext, CancellationToken cancellationToken)
    {
        ActivityExecutionContext = activityExecutionContext;
        SourceActivityExecutionContext = sourceActivityExecutionContext;
        CancellationToken = cancellationToken;
    }

    public ActivityExecutionContext ActivityExecutionContext { get; init; }
    public ActivityExecutionContext SourceActivityExecutionContext { get; init; }
    public CancellationToken CancellationToken { get; init; }
    internal bool StopPropagationRequested { get; private set; }

    /// <summary>
    /// Stops the signal from propagating further up the activity execution context hierarchy.
    /// </summary>
    public void StopPropagation() => StopPropagationRequested = true;
}