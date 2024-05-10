namespace Elsa.Workflows.ComponentTests.Helpers.Activities;

public class TriggerSignal(object signal) : CodeActivity
{
    public object Signal { get; set; } = signal;

    protected override void Execute(ActivityExecutionContext context)
    {
        var signalManager = context.GetRequiredService<ISignalManager>();
        signalManager.Trigger(Signal);
    }
}