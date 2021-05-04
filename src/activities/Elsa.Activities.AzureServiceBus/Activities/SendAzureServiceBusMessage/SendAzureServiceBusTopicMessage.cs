using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Topic Message", Description = "Sends a message to the specified topic", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusTopicMessage : Activity
    {
        private readonly ITopicMessageSenderFactory _messageSenderFactory;
        private readonly IContentSerializer _serializer;

        public SendAzureServiceBusTopicMessage(ITopicMessageSenderFactory messageSenderFactory, IContentSerializer serializer)
        {
            _messageSenderFactory = messageSenderFactory;
            _serializer = serializer;
        }

        [ActivityProperty] public string TopicName { get; set; } = default!;
        [ActivityProperty] public object Message { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var sender = await _messageSenderFactory.GetTopicSenderAsync(TopicName, context.CancellationToken);

            var message = Extensions.MessageBodyExtensions.CreateMessage(_serializer,Message);

            if (!string.IsNullOrWhiteSpace(context.WorkflowExecutionContext.CorrelationId))
                message.CorrelationId = context.WorkflowExecutionContext.CorrelationId;

            await sender.SendAsync(message);
            return Done();
        }
    }
}