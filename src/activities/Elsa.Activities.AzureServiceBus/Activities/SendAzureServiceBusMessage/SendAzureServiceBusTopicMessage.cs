using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Topic Message", Description = "Sends a message to the specified topic", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusTopicMessage : AzureServiceBusSendActivity
    {
        private readonly ITopicMessageSenderFactory _messageSenderFactory;

        public SendAzureServiceBusTopicMessage(ITopicMessageSenderFactory messageSenderFactory, IContentSerializer serializer)
            : base(serializer) =>
            _messageSenderFactory = messageSenderFactory;

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string TopicName { get; set; } = default!;

        protected override Task<ISenderClient> GetSenderAsync() => _messageSenderFactory.GetTopicSenderAsync(TopicName);
    }
}