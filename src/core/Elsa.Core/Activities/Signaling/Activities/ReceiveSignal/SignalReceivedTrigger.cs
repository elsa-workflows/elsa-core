using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class SignalReceivedTrigger : Trigger
    {
        public string Signal { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class SignalReceivedTriggerProvider : TriggerProvider<SignalReceivedTrigger, SignalReceived>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<SignalReceived> context, CancellationToken cancellationToken) =>
            new SignalReceivedTrigger
            {
                Signal = (await context.Activity.GetPropertyValueAsync(x => x.Signal, cancellationToken))!,
                CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
            };
    }
}