using Elsa.Testing.Shared.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;

namespace Elsa.Testing.Shared.Activities;

public class TriggerSignal(string signal) : CodeActivity
{
    public string Signal { get; set; } = signal;

    protected override void Execute(ActivityExecutionContext context)
    {
        var signalManager = context.GetRequiredService<SignalManager>();
        context.DeferTask(() => signalManager.Trigger(Signal));
    }
}