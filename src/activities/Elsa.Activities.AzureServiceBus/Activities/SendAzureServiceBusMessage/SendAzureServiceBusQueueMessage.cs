using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Message", Description = "Sends a message to the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusQueueMessage : AzureServiceBusSendActivity
    {
        private readonly IQueueMessageSenderFactory _queueMessageSenderFactory;

        public SendAzureServiceBusQueueMessage(IQueueMessageSenderFactory queueMessageSenderFactory, IContentSerializer serializer)
            : base(serializer) =>
            _queueMessageSenderFactory = queueMessageSenderFactory;

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string QueueName { get; set; } = default!;

        protected override Task<ISenderClient> GetSenderAsync() => _queueMessageSenderFactory.GetSenderAsync(QueueName);
    }
}