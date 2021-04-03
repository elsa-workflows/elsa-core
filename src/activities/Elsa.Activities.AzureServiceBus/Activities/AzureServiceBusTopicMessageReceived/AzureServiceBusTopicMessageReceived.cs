using System;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    [Trigger(Category = "Azure Service Bus", DisplayName = "Service Bus Topic Message Received", Description = "Triggered when a message is received on the specified topic/subscription", Outcomes = new[] { OutcomeNames.Done })]
    public class AzureServiceBusTopicMessageReceived : Activity
    {
        private readonly IContentSerializer _serializer;

        public AzureServiceBusTopicMessageReceived(IContentSerializer serializer)
        {
            _serializer = serializer;
        }

        [ActivityProperty(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string TopicName { get; set; } = default!;

        [ActivityProperty(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string SubscriptionName { get; set; } = default!;

        [ActivityProperty]
        public Type MessageType { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            var message = (MessageModel) context.Input!;
            var model = message.ReadBody(MessageType, _serializer);

            return Done(model);
        }
    }
}