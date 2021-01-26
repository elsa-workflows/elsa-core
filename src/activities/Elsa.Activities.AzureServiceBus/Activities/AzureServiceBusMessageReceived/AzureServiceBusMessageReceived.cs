﻿using System;
using System.Text;
using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Azure.ServiceBus;

namespace Elsa.Activities.AzureServiceBus
{
    [Trigger(Category = "Azure Service Bus", DisplayName = "Service Bus Message Received", Description = "Triggered when a message is received on the specified queue", Outcomes = new[] { OutcomeNames.Done })]
    public class AzureServiceBusMessageReceived : Activity
    {
        private readonly IContentSerializer _serializer;

        public AzureServiceBusMessageReceived(IContentSerializer serializer)
        {
            _serializer = serializer;
        }

        [ActivityProperty] public string QueueName { get; set; } = default!;
        [ActivityProperty] public Type MessageType { get; set; } = default!;

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