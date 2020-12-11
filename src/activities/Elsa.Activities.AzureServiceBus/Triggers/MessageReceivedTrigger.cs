using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Activities.AzureServiceBus.Triggers
{
    public class MessageReceivedTrigger : Trigger
    {
        public string QueueName { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedTriggerProvider : TriggerProvider<MessageReceivedTrigger, AzureServiceBusMessageReceived>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<AzureServiceBusMessageReceived> context, CancellationToken cancellationToken) =>
            new MessageReceivedTrigger
            {
                QueueName = (await context.Activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken))!,
                CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
            };
    }
}