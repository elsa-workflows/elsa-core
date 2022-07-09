using System;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.AzureServiceBus
{
    [Trigger(Category = "Azure Service Bus", DisplayName = "Service Bus Message Received", Description = "Triggered when a message is received on the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class AzureServiceBusQueueMessageReceived : Activity
    {
        private readonly IContentSerializer _serializer;

        public AzureServiceBusQueueMessageReceived(IContentSerializer serializer)
        {
            _serializer = serializer;
        }

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string QueueName { get; set; } = default!;

        [ActivityInput] public Type MessageType { get; set; } = default!;

        [ActivityOutput] public object? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            var message = (MessageModel)context.Input!;
            Output = message.ReadBody(MessageType, _serializer);

            context.LogOutputProperty(this, nameof(Output), Output);
            context.JournalData.Add("Headers", message.ExtractHeaders());

            return Done();
        }
    }
}