using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class ReceiveSignalTrigger : Trigger
    {
        public string Signal { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class ReceiveSignalTriggerProvider : TriggerProvider<ReceiveSignalTrigger, ReceiveSignal>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<ReceiveSignal> context, CancellationToken cancellationToken) =>
            new ReceiveSignalTrigger
            {
                Signal = await context.Activity.GetPropertyValueAsync(x => x.Signal, cancellationToken),
                CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
            };
    }
}