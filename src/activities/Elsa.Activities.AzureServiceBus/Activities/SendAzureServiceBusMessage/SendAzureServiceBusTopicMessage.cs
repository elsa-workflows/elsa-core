using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Topic Message", Description = "Sends a message to the specified topic", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusTopicMessage : AzureServiceBusSendActivity
    {
        public SendAzureServiceBusTopicMessage(IContentSerializer serializer) : base(serializer)
        {
        }

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string TopicName { get; set; } = default!;

        protected override string? GetTopic() => TopicName;
    }
}