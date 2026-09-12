using Elsa.Workflows.Signals;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals.Activities;

/// <summary>
/// An activity that throws and also claims its own <see cref="FaultSignal"/>, used to pin the channel's self-receipt
/// behavior: a signal is delivered to its sender before the walk up the ancestor chain begins.
/// </summary>
public class SelfHandlingFaultingActivity : CodeActivity
{
    public SelfHandlingFaultingActivity()
    {
        OnSignalReceived<FaultSignal>((_, context) => context.StopPropagation());
    }

    protected override void Execute(ActivityExecutionContext context) => throw new InvalidOperationException("Whoops!");
}
