using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Activities.AzureServiceBus.Triggers
{
    public class MessageReceivedTrigger : ITrigger
    {
        public MessageReceivedTrigger()
        {
        }

        public MessageReceivedTrigger(string queueName, string? correlationId = default)
        {
            QueueName = queueName;
            CorrelationId = correlationId;
        }
        
        public string QueueName { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedWorkflowTriggerProvider : WorkflowTriggerProvider<MessageReceivedTrigger, AzureServiceBusMessageReceived>
    {
        public override async ValueTask<IEnumerable<ITrigger>> GetTriggersAsync(TriggerProviderContext<AzureServiceBusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new MessageReceivedTrigger
                {
                    QueueName = (await context.Activity.GetPropertyValueAsync(x => x.QueueName, cancellationToken))!,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}