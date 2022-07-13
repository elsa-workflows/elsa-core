using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;

namespace Elsa.Activities.AzureServiceBus
{
    [Action(Category = "Azure Service Bus", DisplayName = "Send Service Bus Message", Description = "Sends a message to the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class SendAzureServiceBusQueueMessage : AzureServiceBusSendActivity
    {
        public SendAzureServiceBusQueueMessage(IContentSerializer serializer) : base(serializer)
        {
        }

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string QueueName { get; set; } = default!;

        protected override string? GetQueue() => QueueName;
    }
}