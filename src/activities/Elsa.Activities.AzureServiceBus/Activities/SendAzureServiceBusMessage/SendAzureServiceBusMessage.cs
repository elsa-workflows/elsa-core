using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus;

namespace Elsa.Activities.AzureServiceBus
{
    [Trigger(Category = "Azure Service Bus", DisplayName = "Send Service Bus Message", Description = "Sends a message to the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusMessage : Activity
    {
        private readonly IMessageSenderFactory _messageSenderFactory;
        private readonly IContentSerializer _serializer;

        public SendAzureServiceBusMessage(IMessageSenderFactory messageSenderFactory, IContentSerializer serializer)
        {
            _messageSenderFactory = messageSenderFactory;
            _serializer = serializer;
        }

        [ActivityProperty] public string QueueName { get; set; } = default!;
        [ActivityProperty] public object Message { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var sender = await _messageSenderFactory.GetSenderAsync(QueueName, context.CancellationToken);
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