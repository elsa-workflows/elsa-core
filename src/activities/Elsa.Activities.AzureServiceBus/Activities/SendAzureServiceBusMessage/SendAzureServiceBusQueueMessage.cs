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
    public class SendAzureServiceBusQueueMessage : AzureServiceBusSendActivity
    {
        private readonly IQueueMessageSenderFactory _queueMessageSenderFactory;

        public SendAzureServiceBusQueueMessage(IQueueMessageSenderFactory queueMessageSenderFactory, IContentSerializer serializer)
            :base(serializer)
        {
            _queueMessageSenderFactory = queueMessageSenderFactory;
        }

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string QueueName { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            Sender = await _queueMessageSenderFactory.GetSenderAsync(QueueName, context.CancellationToken);
            return await base.OnExecuteAsync(context);
        }
    }
}