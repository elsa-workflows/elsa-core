using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals.Activities;

/// <summary>
/// A sequential container that delegates <see cref="FaultSignal"/> handling to a caller-supplied strategy, so that a
/// single activity type covers every case under test: declining a fault, claiming it, and claiming it incorrectly.
/// </summary>
public class FaultHandlingContainer : TestContainer
{
    private readonly Func<FaultSignal, SignalContext, ValueTask>? _onChildFaulted;

    /// <param name="onChildFaulted">
    /// Invoked for every fault bubbling through this container. Passing <c>null</c> declines every fault, which lets it
    /// keep bubbling to the next ancestor.
    /// </param>
    public FaultHandlingContainer(Func<FaultSignal, SignalContext, ValueTask>? onChildFaulted = null)
    {
        _onChildFaulted = onChildFaulted;
        OnSignalReceived<FaultSignal>(OnChildFaultedAsync);
    }

    /// <summary>
    /// The number of faults this container was notified of.
    /// </summary>
    public int FaultsSeen { get; private set; }

    private async ValueTask OnChildFaultedAsync(FaultSignal signal, SignalContext context)
    {
        FaultsSeen++;

        if (_onChildFaulted != null)
            await _onChildFaulted(signal, context);
    }
}
