using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus;
using IServiceBusFactory = Elsa.Activities.AzureServiceBus.Services.IServiceBusFactory;

namespace Elsa.Activities.AzureServiceBus.Activities
{
    [Trigger(Category = "Azure Service Bus", DisplayName = "Send Service Bus Message", Description = "Sends a message to the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusMessage : Activity
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly IContentSerializer _serializer;

        public SendAzureServiceBusMessage(IServiceBusFactory serviceBusFactory, IContentSerializer serializer)
        {
            _serviceBusFactory = serviceBusFactory;
            _serializer = serializer;
        }

        [ActivityProperty] public string QueueName { get; set; } = default!;
        [ActivityProperty] public object Message { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var sender = await _serviceBusFactory.GetSenderAsync(QueueName, cancellationToken);
            var json = _serializer.Serialize(Message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var message = new Message(bytes);

            if (!string.IsNullOrWhiteSpace(context.WorkflowExecutionContext.CorrelationId))
                message.CorrelationId = context.WorkflowExecutionContext.CorrelationId;

            await sender.SendAsync(message);
            return Done();
        }
    }
}