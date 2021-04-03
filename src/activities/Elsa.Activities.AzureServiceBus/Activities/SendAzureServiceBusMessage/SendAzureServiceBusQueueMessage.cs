using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Message", Description = "Sends a message to the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusQueueMessage : Activity
    {
        private readonly IMessageSenderFactory _messageSenderFactory;
        private readonly IContentSerializer _serializer;

        public SendAzureServiceBusQueueMessage(IMessageSenderFactory messageSenderFactory, IContentSerializer serializer)
        {
            _messageSenderFactory = messageSenderFactory;
            _serializer = serializer;
        }

        [ActivityProperty(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string QueueName { get; set; } = default!;

        [ActivityProperty(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json })]
        public object Message { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var sender = await _messageSenderFactory.GetSenderAsync(QueueName, context.CancellationToken);
            var message = Extensions.MessageBodyExtensions.CreateMessage(_serializer, Message);

            if (!string.IsNullOrWhiteSpace(context.WorkflowExecutionContext.CorrelationId))
                message.CorrelationId = context.WorkflowExecutionContext.CorrelationId;

            await sender.SendAsync(message);
            return Done();
        }
    }
}