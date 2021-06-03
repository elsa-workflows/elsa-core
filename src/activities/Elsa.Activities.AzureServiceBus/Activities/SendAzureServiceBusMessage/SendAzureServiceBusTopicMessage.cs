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
    public class SendAzureServiceBusTopicMessage : AzureServiceBusSendActivity
    {
        private readonly ITopicMessageSenderFactory _messageSenderFactory;

        public SendAzureServiceBusTopicMessage(ITopicMessageSenderFactory messageSenderFactory, IContentSerializer serializer)
            :base(serializer)
        {
            _messageSenderFactory = messageSenderFactory;            
        }

        [ActivityInput] public string TopicName { get; set; } = default!;       

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            Sender = await _messageSenderFactory.GetTopicSenderAsync(TopicName, context.CancellationToken);

            return await base.OnExecuteAsync(context);
        }
    }
}